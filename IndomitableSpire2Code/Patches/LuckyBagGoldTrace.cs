// 可选诊断代码：复制到 Mod 的编译目录后，由现有 Harmony.PatchAll() 注册。
// 只记录数据，不改金币、Cost、方法参数、返回值或网络消息。

using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

internal static class LuckyBagGoldTrace
{
    internal static string Players()
    {
        var run = RunManager.Instance.DebugOnlyGetState();
        return run == null ? "no-run" : string.Join(", ", run.Players.Select(p => $"{p.NetId}:{p.Gold}"));
    }

    internal static int? Gold(ulong playerId) =>
        RunManager.Instance.DebugOnlyGetState()?.GetPlayer(playerId)?.Gold;

    internal static void Write(string text) =>
        Log.Info($"[LuckyBagGoldTrace] local={LocalContext.NetId} {text} players=[{Players()}]");
}

[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.OnTryPurchaseWrapper),
    new[] { typeof(MerchantInventory), typeof(bool) })]
public static class LuckyBagFreePurchaseTrace
{
    [HarmonyPrefix]
    private static void Prefix(MerchantEntry __instance, bool ignoreCost)
    {
        if (ignoreCost && __instance is MerchantRelicEntry relic)
            LuckyBagGoldTrace.Write($"FREE_RELIC_BEGIN relic={relic.Model?.Id} observedCost={relic.Cost}");
    }
}

[HarmonyPatch(typeof(RewardSynchronizer), nameof(RewardSynchronizer.SyncLocalGoldLost))]
public static class LuckyBagGoldSendTrace
{
    [HarmonyPrefix]
    private static void Prefix(int goldLost) => LuckyBagGoldTrace.Write($"SEND amount={goldLost}");
}

[HarmonyPatch(typeof(RewardSynchronizer), "HandleGoldLostMessage")]
public static class LuckyBagGoldReceiveTrace
{
    [HarmonyPrefix]
    private static void Prefix(ulong senderId, out int? __state) => __state = LuckyBagGoldTrace.Gold(senderId);

    [HarmonyPostfix]
    private static void Postfix(GoldLostMessage message, ulong senderId, int? __state) =>
        LuckyBagGoldTrace.Write($"RECEIVE sender={senderId} amount={message.goldLost} " +
            $"before={__state} after={LuckyBagGoldTrace.Gold(senderId)}");
}