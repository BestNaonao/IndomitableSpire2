using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TashkentSpire2.TashkentSpire2Code.Nodes;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch]
public static class TashkentIdleAnimationPatch
{
    [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
    [HarmonyPostfix]
    public static void CreatureReadyPostfix(NCreature __instance)
        => NTashkentIdleAnimation.AttachCombat(__instance);

    // Patch the shared wrappers, not individual cards/potions: covers all skins,
    // automatic plays and replicated remote actions without sending network messages.
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    [HarmonyPrefix]
    public static void CardPlayedPrefix(CardModel __instance)
        => NTashkentIdleAnimation.NotifyActivity(__instance.Owner);

    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.OnUseWrapper))]
    [HarmonyPrefix]
    public static void PotionUsedPrefix(PotionModel __instance)
        => NTashkentIdleAnimation.NotifyActivity(__instance.Owner);
}
