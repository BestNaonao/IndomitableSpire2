using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public class TorpedoPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(40m, ValueProp.Unpowered)];

    public int ComputeTurns()
    {
        var distPower = base.Owner.GetPower<DistancePower>();
        int dist = distPower != null ? (int)distPower.Amount : 0;

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
        base.DynamicVars.Damage.BaseValue = damage;
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
            ? candidates[new Random().Next(candidates.Count)]
            : null;

        if (target == null)
            return;

        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NFireSmokePuffVfx.Create(target));

        await Cmd.CustomScaledWait(0.2f, 0.4f);

        await CreatureCmd.Damage(choiceContext, target, base.DynamicVars.Damage, base.Owner);

        await PowerCmd.Remove(this);
    }
}