using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Cards;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(FranticEscape), "OnPlay")]
public static class FranticEscapeDistancePatch
{
    static async Task Postfix(Task __result, FranticEscape __instance)
    {
        await __result;

        var owner = __instance.Owner?.Creature;
        if (owner == null) return;
        
        var distPower = owner.GetPower<DistancePower>();
        if (distPower != null)
        {
            await distPower.ModifyAmountFromEscape(-1m, __instance);
        }
    }
}