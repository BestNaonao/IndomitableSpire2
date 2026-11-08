using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Helpers;

internal static class DistanceIntentPreview
{
    private readonly record struct PreviewDistance(DistancePower Power, Creature Attacker, int Amount);

    [ThreadStatic] private static PreviewDistance? _preview;
    [ThreadStatic] private static bool _isCalculating;

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
            || intent.DamageCalc == null)
            return false;

        var player = LocalContext.GetMe(combat);
        if (player?.PlayerCombatState?.Phase != PlayerTurnPhase.Play || !player.Creature.IsAlive)
            return false;
        var distance = player.Creature.GetPower<DistancePower>();
        if (distance == null)
            return false;

        int futureDistance = GetProjectedDistance(distance);
        if (futureDistance == distance.Amount)
            return false;

        try
        {
            _isCalculating = true;
            decimal damage = intent.DamageCalc();
            current = CalculateDamage(damage);
            _preview = new PreviewDistance(distance, attacker, futureDistance);
            projected = CalculateDamage(damage);
        }
        finally
        {
            _preview = null;
            _isCalculating = false;
        }
        return current != projected;

        int CalculateDamage(decimal baseDamage) => Math.Max(0, (int)Hook.ModifyDamage(
            player.RunState, combat, player.Creature, attacker, baseDamage, ValueProp.Move,
            null, null, ModifyDamageHookType.All, CardPreviewMode.None, out _));
    }

    private static int GetProjectedDistance(DistancePower distance)
    {
        var owner = distance.Owner;
        var warpDrive = owner.GetPower<WarpDrivePower>();
        long maxDistance = Math.Min(int.MaxValue, 5L + Math.Max(0, warpDrive?.Amount ?? 0));
        int amount = distance.Amount;
        foreach (var power in owner.Powers)
        {
            long delta = power switch
            {
                BackAfterTurnPower => -(long)power.Amount,
                ChargeAfterTurnPower => power.Amount,
                TauntEndTurnPower => -(long)amount,
                _ => 0
            };
            if (delta == 0)
                continue;
            if (warpDrive != null && delta < 0)
                delta = -delta;
            amount = (int)Math.Clamp(amount + delta, -5L, maxDistance);
        }

        return amount;
    }
}
