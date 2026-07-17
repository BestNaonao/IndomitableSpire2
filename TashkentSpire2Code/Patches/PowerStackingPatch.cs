using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(Creature), nameof(Creature.ApplyPowerInternal))]
public static class PowerStackingPatch
{
    public static bool Prefix(Creature __instance, PowerModel power)
    {
        if (power.InstanceType == PowerInstanceType.None && __instance.Powers.Any(p => p.GetType() == power.GetType()))
        {
            var existing = __instance.Powers.First(p => p.GetType() == power.GetType());
            existing.SetAmount(existing.Amount + (int)power.Amount, false);
            return false; 
        }
        return true;
    }
}