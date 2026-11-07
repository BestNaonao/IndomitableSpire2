using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
public static class PoseidonFormBackgroundPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCombatRoom __instance) => NPoseidonFormVfx.Refresh(__instance);
}
