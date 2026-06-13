using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
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
        __result = ReplacementLogic(__instance, choiceContext, cardPlay);
        return false;
    }

    private static async Task ReplacementLogic(MonologuePower instance, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var internalData = Traverse.Create(instance).Field("_internalData").GetValue();
        if (internalData == null) return;

        var dictField = AccessTools.Field(internalData.GetType(), "amountsForPlayedCards");
        var dict = dictField?.GetValue(internalData) as Dictionary<CardModel, int>;

        if (dict != null && cardPlay.Card.Owner == instance.Owner.Player && dict.Remove(cardPlay.Card, out var value))
        {
            Traverse.Create(instance).Method("Flash").GetValue();

            int finalValue = value * instance.Amount; 
            await PowerCmd.Apply<StrengthPower>(choiceContext, instance.Owner, (decimal)finalValue, instance.Owner, null, silent: true);

            instance.DynamicVars["StrengthApplied"].BaseValue += (decimal)finalValue;

            Traverse.Create(instance).Method("InvokeDisplayAmountChanged").GetValue();
        }
    }

    [HarmonyPatch(nameof(MonologuePower.AfterSideTurnEnd))]
    [HarmonyPrefix]
    static bool AfterSideTurnEndPrefix(MonologuePower __instance, PlayerChoiceContext choiceContext, CombatSide side, ref Task __result)
    {
        if (side == __instance.Owner.Side)
        {
            __result = ClearStrengthAsync(__instance, choiceContext);
            return false;
        }
        return true;
    }

    private static async Task ClearStrengthAsync(MonologuePower instance, PlayerChoiceContext choiceContext)
    {
        decimal totalToClear = instance.DynamicVars["StrengthApplied"].BaseValue;
        
        await PowerCmd.Remove(instance);
        if (totalToClear != 0)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, instance.Owner, -totalToClear, instance.Owner, null, silent: true);
        }
    }
}