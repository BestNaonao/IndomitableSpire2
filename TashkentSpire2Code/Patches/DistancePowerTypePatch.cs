using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.GetTypeForAmount))]
public static class DistancePowerTypePatch
{
    [HarmonyPrefix]
    public static bool Prefix(PowerModel __instance, decimal customAmount, ref PowerType __result)
    {
        if (__instance is DistancePower)
        {
            __result = PowerType.Buff;
    
            return false; 
        }
    
        return true;
    }
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.ShouldRemoveDueToAmount))]
public static class PersistentPowerPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PowerModel __instance, ref bool __result)
    {
        if (__instance is IPersistentPower)
        {
            __result = false;

            return false;
        }
        return true;
    }
}