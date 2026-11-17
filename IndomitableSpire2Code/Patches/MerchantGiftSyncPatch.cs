using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

// 0.113 的购买逻辑不再发送奖励消息。只为本地赠品恢复旧版的入包同步时点；
// 普通付费交易仍由原版 MerchantSynchronizer 负责。
[HarmonyPatch(typeof(MerchantRelicEntry), "OnTryPurchaseInternal")]
public static class MerchantGiftSyncPatch
{
    [HarmonyPostfix]
    private static void Postfix(MerchantRelicEntry __instance, bool ignoreCost, ref Task<(bool, int)> __result)
    {
        if (ignoreCost && CustomMerchantCmd.IsGiftClaim(__instance) && __instance.Model is { } relic)
            __result = SyncAfterObtain(__result, relic);
    }
    
    private static async Task<(bool, int)> SyncAfterObtain(Task<(bool, int)> original, RelicModel relic)
    {
        var result = await original;
        if (result.Item1) RunManager.Instance.RewardSynchronizer.SyncLocalObtainedRelic(relic);
        return result;
    }
}