using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;
using TashkentSpire2.TashkentSpire2Code.Commands;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class TorpedoPower : TashkentPower
{
    private const string TurnKey = "Turns";

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => true;

    public override string CustomBigIconPath =>
        "res://TashkentSpire2/images/powers/big/torpedo_power.png";
    public override string CustomPackedIconPath =>
        "res://TashkentSpire2/images/powers/packed/torpedo_power.png";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(0m, ValueProp.Unpowered),
        new DynamicVar(TurnKey, 0m)
    ];

    private bool _oxygenApplied;

    public override int DisplayAmount => (int)DynamicVars[TurnKey].BaseValue;

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
    
    public void ReduceTurnCount(int amount)
    {
        var turnVar = DynamicVars[TurnKey];
        turnVar.BaseValue = Math.Max(1, turnVar.BaseValue - amount);
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (!_oxygenApplied)
        {
            _oxygenApplied = true;

            int oxygenBonus = Owner?.GetPower<OxygenTorpedoPower>() is { } oxy
                ? (int)oxy.Amount
                : 0;

            if (oxygenBonus != 0)
            {
                await PowerCmd.ModifyAmount(this, oxygenBonus, Owner, cardSource);
            }
        }

        var allRounderPower = Owner?.GetPower<AllRounderPower>();
        if (allRounderPower != null && allRounderPower.Amount > 0)
        {
            await CreatureCmd.GainBlock(Owner!, allRounderPower.Amount, ValueProp.Unpowered, null);
        }

        int turns = ComputeTurns(Owner!);
        DynamicVars[TurnKey].BaseValue = turns;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;

        int turns = (int)DynamicVars[TurnKey].BaseValue;

        if (turns <= 0)
        {
            turns = ComputeTurns(Owner!);
            DynamicVars[TurnKey].BaseValue = turns;
            InvokeDisplayAmountChanged();
        }

        if (turns > 1)
        {
            turns--;
            DynamicVars[TurnKey].BaseValue = turns;
            InvokeDisplayAmountChanged();
            return;
        }

        Flash();
        await Cmd.CustomScaledWait(0.2f, 0.4f);

        var enemies = CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0)
            return;

        var godPower = Owner?.GetPower<TorpedoGodPower>();

        if (godPower != null)
        {
            var dmg = new DamageVar(Amount, ValueProp.Unpowered);
            await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, dmg, Owner!);

            foreach (var e in CombatState.HittableEnemies)
            {
                await ApplyFlooding(choiceContext, e);
            }
        }
        else
        {
            var target = SelectTarget(enemies);
            if (target == null)
                return;

            var dmg = new DamageVar(Amount, ValueProp.Unpowered);
            await CreatureCmd.Damage(choiceContext, target, dmg, Owner!);

            await ApplyFlooding(choiceContext, target);
        }
        await TriggerReload(choiceContext);

        await PowerCmd.Remove(this);
    }

    private Creature? SelectTarget(List<Creature> enemies)
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

        return candidates.Count > 0
            ? Owner?.Player?.RunState.Rng.CombatTargets.NextItem(candidates)
            : null;
    }

    private async Task ApplyFlooding(PlayerChoiceContext ctx, Creature target)
    {
        int floodingAmount = Owner?.GetPower<FloodingExpertPower>() is { } flood
            ? (int)flood.Amount
            : 0;

        if (floodingAmount <= 0)
            return;

        await PowerCmd.Apply<WeakPower>(target, floodingAmount, Owner, null);
        await PowerCmd.Apply<VulnerablePower>(target, floodingAmount, Owner, null);
        await PowerCmd.Apply<MarkPower>(target, floodingAmount, Owner, null);
    }

    private async Task TriggerReload(PlayerChoiceContext ctx)
    {
        var reloadCards = Owner?.Player?.Piles
            .SelectMany(p => p.Cards)
            .OfType<TorpedoReload>();

        if (reloadCards == null)
            return;

        foreach (var card in reloadCards)
        {
            int load = card.DynamicVars["TashkentSpire2-Load"].IntValue;
            await Loadcmd.Execute(ctx, card, load);
        }
    }
}