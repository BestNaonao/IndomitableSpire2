using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;
using TashkentSpire2.TashkentSpire2Code.Commands;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class TorpedoPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => true;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/torpedo_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/torpedo_power.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(0m, ValueProp.Unpowered)];
    
    public static int ComputeTurns(Creature owner)
    {
        var distPower = owner.GetPower<DistancePower>();
        int dist = distPower != null ? (int)distPower.Amount : 0;
        dist -= 10;
        if (distPower == null || dist == -1 || dist == 0 || dist == 1)
            return 3;
        if (dist == -2 || dist == -3)
            return 4;
        if (dist == -4 || dist == -5)
            return 5;
        if (dist == 2 || dist == 3)
            return 2;
        if (dist == 4 || dist == 5)
            return 1;

        return 3;
    }
    
    public void SetDamage(decimal damage)
    {
        AssertMutable();
        int oxygenBonus = (int)(Owner?.GetPower<OxygenTorpedoPower>()?.Amount ?? 0m);
        base.DynamicVars.Damage.BaseValue = damage + oxygenBonus;
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Side)
            return;

        if (base.Amount > 1)
        {
            await PowerCmd.Decrement(this);
            return;
        }

        Flash();
        await Cmd.CustomScaledWait(0.2f, 0.4f);

        var enemies = base.CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0)
            return;

        var godPower = base.Owner?.GetPower<TorpedoGodPower>();

        if (godPower != null)
        {
            foreach (var e in enemies)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NFireSmokePuffVfx.Create(e));
                await Cmd.CustomScaledWait(0.2f, 0.4f);

                await CreatureCmd.Damage(choiceContext, e, base.DynamicVars.Damage, base.Owner!);

                int floodingAmount = (int)(base.Owner?.GetPower<FloodingExpertPower>()?.Amount ?? 0m);
                if (floodingAmount > 0)
                {
                    await PowerCmd.Apply<WeakPower>(e, (decimal)floodingAmount, base.Owner, null);
                    await PowerCmd.Apply<VulnerablePower>(e, (decimal)floodingAmount, base.Owner, null);
                    await PowerCmd.Apply<MarkPower>(e, (decimal)floodingAmount, base.Owner, null);
                }
            }
        }
        else
        {
            var enemyMarks = enemies
                .Select(e => new
                {
                    Enemy = e,
                    Mark = (int)(e.GetPower<MarkPower>()?.Amount ?? 0m)
                })
                .ToList();

            int maxMark = enemyMarks.Max(x => x.Mark);

            var candidates = enemyMarks
                .Where(x => x.Mark == maxMark)
                .Select(x => x.Enemy)
                .ToList();

            var target = candidates.Count > 0
                ? base.Owner?.Player?.RunState.Rng.CombatTargets.NextItem(candidates)
                : null;

            if (target == null)
                return;

            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NFireSmokePuffVfx.Create(target));

            await Cmd.CustomScaledWait(0.2f, 0.4f);

            await CreatureCmd.Damage(choiceContext, target, base.DynamicVars.Damage, base.Owner!);
        
            int floodingAmount = (int)(base.Owner?.GetPower<FloodingExpertPower>()?.Amount ?? 0m);
            if (floodingAmount > 0)
            {
                await PowerCmd.Apply<WeakPower>(target, (decimal)floodingAmount, base.Owner, null);
                await PowerCmd.Apply<VulnerablePower>(target, (decimal)floodingAmount, base.Owner, null);
                await PowerCmd.Apply<MarkPower>(target, (decimal)floodingAmount, base.Owner, null);
            }
        }

        var reloadCards = base.Owner?.Player?.Piles
            .SelectMany(p => p.Cards)
            .OfType<TorpedoReload>();

        if (reloadCards != null)
        {
            foreach (var card in reloadCards)
            {
                int load = card.DynamicVars["TashkentSpire2-Load"].IntValue;
                await Loadcmd.Execute(choiceContext, card, load);
            }
        }
        
        await PowerCmd.Remove(this);
    }
}