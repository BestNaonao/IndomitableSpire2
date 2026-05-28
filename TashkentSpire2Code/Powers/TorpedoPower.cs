using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Extensions;
using TashkentSpire2.TashkentSpire2Code.Relics;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class TorpedoPower : TashkentPower, IHasSecondAmount
{
    private const string TurnKey = "Turns";
    private const string BombKey = "IsTheBomb";

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
        new DynamicVar(TurnKey, 0m),
        new DynamicVar(BombKey, 0m),
        new DynamicVar("AOEFlag", 0m)
    ];

    private bool _oxygenApplied;
    private bool IsTheBomb => DynamicVars[BombKey].BaseValue == 1m;

    public override LocString Title
    {
        get
        {
            LocString title = base.Title;
            title.Add("IsTheBomb", IsTheBomb ? 1 : 0);
            return title;
        }
    }

    public override int DisplayAmount => (int)DynamicVars[TurnKey].BaseValue;

    public string GetSecondAmount()
    {
        return Amount.ToString();
    }

    public void SetIsTheBomb(bool value)
    {
        DynamicVars[BombKey].BaseValue = value ? 1m : 0m;
        SyncAOEFlag();
    }

    public static int ComputeTurns(Creature owner)
    {
        var distPower = owner.GetPower<DistancePower>();
        int dist = distPower != null ? (int)distPower.Amount : 0;
        dist -= 10;

        int baseTurns = 3;

        if (dist == -2 || dist == -3)
            baseTurns = 4;
        else if (dist == -4 || dist == -5)
            baseTurns = 5;
        else if (dist == 2 || dist == 3)
            baseTurns = 2;
        else if (dist == 4 || dist == 5)
            baseTurns = 1;
        else if (distPower == null || dist == -1 || dist == 0 || dist == 1)
            baseTurns = 3;

        int godPowerBonus = (int)(owner.GetPower<TorpedoGodPower>()?.Amount ?? 0m);

        int relicBonus = owner.Player?.Relics.Count(r => r is Thruster) ?? 0;

        return Math.Max(1, baseTurns - godPowerBonus - relicBonus);
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

            int oxygenBonus = Owner?.GetPower<OxygenTorpedoPower>() is { } oxy? (int)oxy.Amount : 0;

            if (oxygenBonus != 0)
            {
                await PowerCmd.ModifyAmount(this, oxygenBonus, Owner, cardSource);
            }
        }

        DynamicVars["AOEFlag"].BaseValue = IsTheBomb || Owner?.GetPower<TorpedoGodPower>() != null ? 1m : 0m;

        if (Owner != null && Owner.Player != null)
        {
            foreach (var allRounderPower in base.Owner.Player.Creature.Powers.OfType<AllRounderPower>())
            {
                allRounderPower.AddCharge(1);
            }
        }

        int turns = ComputeTurns(Owner!);
        DynamicVars[TurnKey].BaseValue = turns;
        InvokeDisplayAmountChanged();
    }
    
    public void SyncAOEFlag()
    {
        DynamicVars["AOEFlag"].BaseValue = IsTheBomb || Owner?.GetPower<TorpedoGodPower>() != null ? 1m : 0m;
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

        IReadOnlyList<Creature> targets;

        bool isAOE = IsTheBomb || godPower != null;

        var dmg = new DamageVar(Amount, ValueProp.Unpowered);

        if (isAOE)
        {
            targets = enemies;

            await CreatureCmd.Damage(choiceContext, targets, dmg, Owner!);
        }
        else
        {
            var target = SelectTarget(enemies);
            if (target == null)
                return;

            targets = [target];

            await CreatureCmd.Damage(choiceContext, target, dmg, Owner!);
        }

        var context = new TorpedoDamageContext
        {
            ChoiceContext = choiceContext,
            Source = Owner!,
            Targets = targets,
            Damage = Amount,
            IsBomb = IsTheBomb,
            IsAOE = isAOE
        };

        await TriggerAfterTorpedoDamage(context);

        await PowerCmd.Remove(this);
    }

    private Creature? SelectTarget(List<Creature> enemies)
    {
        var enemyMarks = enemies.Select(e => new
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

    private async Task TriggerAfterTorpedoDamage(TorpedoDamageContext context)
    {
        foreach (var hook in GetHooks<IAfterTorpedoDamage>())
        {
            await hook.AfterTorpedoDamage(context);
        }
    }

    private IEnumerable<T> GetHooks<T>()
    {
        foreach (var card in Owner!.Player!.Piles.SelectMany(p => p.Cards))
        {
            if (card is T t)
                yield return t;
        }
        
        foreach (var power in Owner.Powers)
        {
            if (power is T t)
                yield return t;
        }

        foreach (var relic in Owner.Player.Relics)
        {
            if (relic is T t)
                yield return t;
        }
    }
}