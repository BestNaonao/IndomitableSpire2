using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TashkentSpire2.TashkentSpire2Code.Minion;

namespace TashkentSpire2.TashkentSpire2Code.Commands;

public static class MinionSummoncmd
{
    public static async Task Summon(PlayerChoiceContext choiceContext, Player owner, decimal amount, AbstractModel? source)
    {
        ICombatState? combatState = owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        amount = Hook.ModifySummonAmount(combatState, owner, amount, source);
        if (amount <= 0m)
        {
            return;
        }

        Creature? existingLeft = combatState.Allies.FirstOrDefault(c => c.PetOwner == owner && c.Monster is MinionLeft);
        Creature? existingRight = combatState.Allies.FirstOrDefault(c => c.PetOwner == owner && c.Monster is MinionRight);
        
        var surrounded = owner.Creature.GetPower<SurroundedPower>();
        bool isFacingLeft = surrounded != null && surrounded.Facing == SurroundedPower.Direction.Left;

        MinionPosition targetLeftPos = isFacingLeft ? MinionPosition.Front : MinionPosition.Back;
        MinionPosition targetRightPos = isFacingLeft ? MinionPosition.Back : MinionPosition.Front;
        
        if (existingLeft is { IsAlive: true } || existingRight is { IsAlive: true })
        {
            return;
        }

        bool isRevivingLeft = existingLeft != null;
        bool isRevivingRight = existingRight != null;

        Creature minion1 = existingLeft ?? await MinionCmd.AddMinion<MinionLeft>(
            owner,
            new MinionSummonOptions(Position: targetLeftPos));
            
        Creature minion2 = existingRight ?? await MinionCmd.AddMinion<MinionRight>(
            owner,
            new MinionSummonOptions(Position: targetRightPos));

        if (isRevivingLeft) owner.PlayerCombatState?.AddPetInternal(minion1);
        if (isRevivingRight) owner.PlayerCombatState?.AddPetInternal(minion2);

        await CreatureCmd.SetMaxHp(minion1, amount);
        await CreatureCmd.Heal(minion1, amount, isRevivingLeft);
        
        await CreatureCmd.SetMaxHp(minion2, amount);
        await CreatureCmd.Heal(minion2, amount, isRevivingRight);
        
        await SyncSpawnMirrorDirection(owner.Creature, minion1);
        await SyncSpawnMirrorDirection(owner.Creature, minion2);
        
        CombatManager.Instance.History.Summoned(combatState, (int)amount, owner);
        await Hook.AfterSummon(combatState, choiceContext, owner, amount);
    }
    
    private static async Task SyncSpawnMirrorDirection(Creature owner, Creature minion)
    {
        await Task.Yield();

        bool shouldFlip = false;

        var dirPower = owner.Powers.FirstOrDefault(p => p.GetType().GetProperty("Facing") != null);
        if (dirPower != null)
        {
            var facingProp = dirPower.GetType().GetProperty("Facing");
            var facingValue = facingProp?.GetValue(dirPower);
            if (facingValue != null && facingValue.ToString() == "Left")
            {
                shouldFlip = true;
            }
        }
        else
        {
            var ownerBody = NCombatRoom.Instance?.GetCreatureNode(owner)?.Body;
            if (ownerBody != null && ownerBody.Scale.X < 0f)
            {
                shouldFlip = true;
            }
        }

        if (shouldFlip)
        {
            Node2D? minionBody = NCombatRoom.Instance?.GetCreatureNode(minion)?.Body;
            if (minionBody != null && minionBody.Scale.X > 0f)
            {
                minionBody.Scale *= new Vector2(-1f, 1f);
            }
        }
    }
}