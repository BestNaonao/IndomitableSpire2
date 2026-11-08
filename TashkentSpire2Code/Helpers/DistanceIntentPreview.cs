using System.Collections.Concurrent;
using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Powers;
using TashkentSpire2.TashkentSpire2Code.Relics;

namespace TashkentSpire2.TashkentSpire2Code.Helpers;

internal static class DistanceIntentPreview
{
    private readonly record struct PreviewDistance(DistancePower Power, Creature Attacker, int Amount);

    [ThreadStatic] private static PreviewDistance? _preview;
    [ThreadStatic] private static bool _isCalculating;
    private static readonly ConcurrentDictionary<Type, bool> UncertainTypes = new();
    private static readonly FieldInfo? RepeatCalculator = typeof(MultiAttackIntent)
        .GetField("_repeatCalc", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly HashSet<Type> KnownSafeTypes =
    [
        typeof(DistancePower), typeof(BackAfterTurnPower), typeof(ChargeAfterTurnPower),
        typeof(TauntEndTurnPower), typeof(TauntPower), typeof(SmokePower), typeof(DodgePower),
        typeof(WeakPower), typeof(VulnerablePower), typeof(IntangiblePower),
        typeof(Scope), typeof(IlluminationRoundPower), typeof(PoseidonFormPower),
        typeof(SpecialServicesPower), typeof(TakeAsBaitPower)
    ];
    private static readonly string[] UncertainHooks =
    [
        nameof(AbstractModel.BeforeSideTurnStart), nameof(AbstractModel.AfterSideTurnStart),
        nameof(AbstractModel.AfterSideTurnStartLate), nameof(AbstractModel.BeforeSideTurnEndVeryEarly),
        nameof(AbstractModel.BeforeSideTurnEndEarly), nameof(AbstractModel.BeforeSideTurnEnd),
        nameof(AbstractModel.AfterSideTurnEnd), nameof(AbstractModel.AfterSideTurnEndLate),
        nameof(AbstractModel.AfterAutoPostPlayPhaseEntered), nameof(AbstractModel.ShouldTakeExtraTurn),
        nameof(AbstractModel.BeforeFlush), nameof(AbstractModel.BeforeFlushLate), nameof(AbstractModel.AfterFlush),
        nameof(AbstractModel.AfterCardDiscarded), nameof(AbstractModel.AfterCardExhausted),
        nameof(AbstractModel.BeforePowerAmountChanged), nameof(AbstractModel.AfterPowerAmountChanged),
        nameof(AbstractModel.ModifyPowerAmountGivenAdditive), nameof(AbstractModel.ModifyPowerAmountGivenMultiplicative),
        nameof(AbstractModel.TryModifyPowerAmountReceived)
    ];

    internal static int ReadDistance(DistancePower power, Creature? target, Creature? dealer) =>
        _preview is { } preview && ReferenceEquals(preview.Power, power)
            && ReferenceEquals(target, power.Owner) && ReferenceEquals(dealer, preview.Attacker)
                ? preview.Amount : power.Amount;

    internal static bool TryGetDamage(AttackIntent intent, Creature attacker, out int current, out int projected)
    {
        current = projected = 0;
        var manager = CombatManager.Instance;
        var combat = attacker.CombatState;
        if (_isCalculating || combat == null || !manager.IsInProgress || manager.IsOverOrEnding
            || manager.EndingPlayerTurnPhaseOne || manager.EndingPlayerTurnPhaseTwo
            || combat.CurrentSide != CombatSide.Player || !attacker.IsEnemy || !attacker.IsAlive
            || attacker.Monster?.GetType().Assembly != typeof(MonsterModel).Assembly
            || (intent.GetType() != typeof(SingleAttackIntent) && intent.GetType() != typeof(MultiAttackIntent)))
            return false;

        var player = LocalContext.GetMe(combat);
        if (player?.PlayerCombatState?.Phase != PlayerTurnPhase.Play || !player.Creature.IsAlive)
            return false;
        var distance = player.Creature.GetPower<DistancePower>();
        if (distance == null || distance.Amount is < -5 or > 5)
            return false;

        int futureDistance = distance.Amount;
        foreach (var power in player.Creature.Powers)
        {
            futureDistance = power switch
            {
                BackAfterTurnPower => (int)Math.Clamp((long)futureDistance - power.Amount, -5L, 5L),
                ChargeAfterTurnPower => (int)Math.Clamp((long)futureDistance + power.Amount, -5L, 5L),
                TauntEndTurnPower => 0,
                _ => futureDistance
            };
        }
        if (futureDistance == distance.Amount || intent.DamageCalc == null)
            return false;
        Type? calculatorType = intent.DamageCalc.Method.DeclaringType;
        if ((calculatorType != intent.GetType() && calculatorType?.DeclaringType != intent.GetType())
            || (intent is MultiAttackIntent && (RepeatCalculator == null || RepeatCalculator.GetValue(intent) != null)))
            return false;

        foreach (var power in player.Creature.Powers.Concat(attacker.Powers))
        {
            if (power is WarpDrivePower or SandpitPower or SurroundedPower or BackAttackLeftPower
                or DefendTerritorialWatersPower || UncertainTypes.GetOrAdd(power.GetType(), HasUncertainHooks))
                return false;
        }
        if (combat.Enemies.Any(enemy => enemy.HasPower<SandpitPower>()))
            return false;

        try
        {
            _isCalculating = true;
            decimal damage = intent.DamageCalc();
            current = CalculateDamage(damage, out var currentModifiers);
            _preview = new PreviewDistance(distance, attacker, futureDistance);
            projected = CalculateDamage(damage, out var projectedModifiers);
            if (currentModifiers.Concat(projectedModifiers)
                .Any(model => UncertainTypes.GetOrAdd(model.GetType(), HasUncertainHooks)))
                return false;
        }
        finally
        {
            _preview = null;
            _isCalculating = false;
        }
        return current != projected;

        int CalculateDamage(decimal baseDamage, out IEnumerable<AbstractModel> modifiers) => Math.Max(0, (int)Hook.ModifyDamage(
            player.RunState, combat, player.Creature, attacker, baseDamage, ValueProp.Move,
            null, null, ModifyDamageHookType.All, CardPreviewMode.None, out modifiers));
    }

    private static bool HasUncertainHooks(Type type)
    {
        if (KnownSafeTypes.Contains(type))
            return false;
        return UncertainHooks.Any(Overrides);

        bool Overrides(string name) => type.GetMethod(name)?.DeclaringType != typeof(AbstractModel);
    }
}
