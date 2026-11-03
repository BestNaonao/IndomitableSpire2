using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Cards;
using TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

/// <summary>Attach once per command, fire once per actual attack iteration, not per enemy.</summary>
[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute), [typeof(PlayerChoiceContext)])]
public static class TashkentAmmunitionMuzzlePatch
{
    [HarmonyPrefix]
    public static void Prefix(AttackCommand __instance, ref Func<Task>? ____beforeDamage)
    {
        if (TestMode.IsOn || __instance.ModelSource is not CardModel { Type: CardType.Attack } card
            || card is not IAmmunitionCard || __instance.Attacker != card.Owner.Creature
            || !__instance.DamageProps.IsPoweredAttack())
            return;

        // Execute may be called again on the same command. Do not stack our own
        // wrapper and double its flashes. Preserve any original damage callback.
        if (____beforeDamage?.Target is ShotCallback callback && callback.Attack == __instance)
            return;
        ____beforeDamage = new ShotCallback(__instance, ____beforeDamage).Run;
    }

    private sealed class ShotCallback(AttackCommand attack, Func<Task>? original)
    {
        public AttackCommand Attack { get; } = attack;

        public Task Run()
        {
            if (original != null)
                return AfterOriginal();
            Play();
            return Task.CompletedTask;
        }

        private async Task AfterOriginal()
        {
            await original!();
            Play();
        }

        private void Play()
        {
            if (Attack.Attacker is { } attacker)
                NAmmunitionMuzzleVfx.Play(attacker);
        }
    }
}
