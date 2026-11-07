using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class TashkentIntangibleVfxPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance) => NTashkentIntangibleVfx.Attach(__instance);
}
