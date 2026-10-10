using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

// 原版没有降级前钩子，且会在 AfterDowngraded 前丢弃所有动态变量。
// 先保存耐久状态，再由 DurableCard.AfterDowngraded 在附魔重算和 UI 通知前恢复。
[HarmonyPatch(typeof(CardModel), nameof(CardModel.DowngradeInternal))]
public static class DurabilityDowngradePatch
{
    [HarmonyPrefix]
    public static void Prefix(CardModel __instance)
    {
        if (__instance is DurableCard card) card.CaptureDurabilityBeforeDowngrade();
    }
}
