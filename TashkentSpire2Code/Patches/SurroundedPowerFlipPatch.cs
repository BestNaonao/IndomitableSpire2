using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(SurroundedPower), "FaceDirection")]
public static class SurroundedPowerFlipPatch
{
    public static void Postfix(SurroundedPower __instance)
    {
        var owner = __instance.Owner;
        if (owner == null) return;
        
        var distancePower = owner.GetPower<DistancePower>();
        if (distancePower != null)
        {
            _ = distancePower.OnDirectionFlipped();
        }
    }
}