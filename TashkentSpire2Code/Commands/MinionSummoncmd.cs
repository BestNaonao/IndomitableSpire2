using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Minion;

namespace TashkentSpire2.TashkentSpire2Code.Commands;

public static class MinionSummoncmd
{
    public static async Task Summon(PlayerChoiceContext choiceContext, Player owner, decimal amount, AbstractModel? source)
    {
        CombatState? combatState = owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        amount = Hook.ModifySummonAmount(combatState, owner, amount, source);
        if (amount <= 0m)
        {
            return;
        }

        Creature? existing = combatState.Allies.FirstOrDefault(c =>
            c.PetOwner == owner && (c.Monster is MinionLeft || c.Monster is MinionRight));

        if (existing is { IsAlive: true })
        {
            //await CreatureCmd.GainMaxHp(existing, amount);
            return;
        }

        bool isReviving = existing != null;
        Creature minion1 = existing ?? await MinionCmd.AddMinion<MinionLeft>(
            owner,
            new MinionSummonOptions(Position: MinionPosition.Back));
        Creature minion2 = existing ?? await MinionCmd.AddMinion<MinionRight>(
            owner,
            new MinionSummonOptions(Position: MinionPosition.Front));

        if (isReviving)
        {
            owner.PlayerCombatState?.AddPetInternal(minion1);
            owner.PlayerCombatState?.AddPetInternal(minion2);
        }

        await CreatureCmd.SetMaxHp(minion1, amount);
        await CreatureCmd.Heal(minion1, amount, isReviving);
        await CreatureCmd.SetMaxHp(minion2, amount);
        await CreatureCmd.Heal(minion2, amount, isReviving);
        CombatManager.Instance.History.Summoned(combatState, (int)amount, owner);
        await Hook.AfterSummon(combatState, choiceContext, owner, amount);
    }
}