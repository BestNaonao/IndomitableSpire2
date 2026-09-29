using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(MonologuePower))]
public static class MonologuePowerPatch
{
    [HarmonyPatch(nameof(MonologuePower.AfterCardPlayed))]
    [HarmonyPrefix]
    static bool AfterCardPlayedPrefix(MonologuePower __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        if (cardPlay.Card.Owner != __instance.Owner.Player)
            return true;

        var internalData = Traverse.Create(__instance).Field("_internalData").GetValue();
        if (internalData == null)
            return true;

        var dictField = AccessTools.Field(internalData.GetType(), "amountsForPlayedCards");
        var dict = dictField?.GetValue(internalData) as Dictionary<CardModel, int>;
        if (dict == null)
            return true;

        if (!dict.Remove(cardPlay.Card, out var value))
        {
            __result = Task.CompletedTask;
            return false;
        }

        __result = ApplyStrengthAsync(__instance, choiceContext, (decimal)value * __instance.Amount);
        return false;
    }

    private static async Task ApplyStrengthAsync(MonologuePower instance, PlayerChoiceContext choiceContext, decimal strength)
    {
        Traverse.Create(instance).Method("Flash").GetValue();
        await PowerCmd.Apply<StrengthPower>(choiceContext, instance.Owner, strength, instance.Owner, null, silent: true);
        instance.DynamicVars[MonologuePower.strengthAppliedKey].BaseValue += strength;
        Traverse.Create(instance).Method("InvokeDisplayAmountChanged").GetValue();
    }
}
