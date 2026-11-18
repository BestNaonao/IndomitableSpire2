using System.Runtime.CompilerServices;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(MerchantSynchronizer), "PurchaseEntry")]
public static class MerchantTransactionSyncPatch
{
    [HarmonyPrefix]
    private static bool Prefix(MerchantSynchronizer __instance, Player player, uint entryId, ref Task<bool> __result)
    {
        __result = MerchantTransactionQueue.Purchase(__instance, __instance.GetInventoryFor(player), entryId);
        return false; // 真正的购买仍由 Execute 内同一个原版非同步购买方法执行，只是多了串行链。
    }
}

[HarmonyPatch(typeof(MerchantSynchronizer), nameof(MerchantSynchronizer.PurchaseLocalEntry))]
public static class MerchantClosingPurchasePatch
{
    [HarmonyPrefix]
    private static bool Prefix(MerchantSynchronizer __instance, ref Task<bool> __result)
    {
        if (!MerchantTransactionQueue.IsClosing(__instance)) return true;
        __result = Task.FromResult(false); // 离店排水期间本地点"买"被优雅拒绝，而已在途的网络购买仍在尾链里，必须排完。
        return false;
    }
}

[HarmonyPatch(typeof(MerchantSynchronizer), nameof(MerchantSynchronizer.OnMerchantEntered))]
public static class MerchantTransactionResetPatch
{
    [HarmonyPrefix]
    private static void Prefix(MerchantSynchronizer __instance) => MerchantTransactionQueue.Reset(__instance);
}

[HarmonyPatch(typeof(RunManager), "ExitCurrentRoom")]
public static class MerchantTransactionExitPatch
{
    [HarmonyPrefix]
    private static bool Prefix(RunManager __instance, ref Task<AbstractRoom?> __result)
    {
        if (__instance.DebugOnlyGetState()?.CurrentRoom is not MerchantRoom) return true;
        __result = FinishPurchasesThenExit(__instance);
        return false;
    }
    
    private static async Task<AbstractRoom?> FinishPurchasesThenExit(RunManager run)
    {
        await MerchantTransactionQueue.DrainForExit(run.MerchantSynchronizer); // 先排水
        return await OriginalExit(run); // 后原版离店
    }
    
    [HarmonyReversePatch] // 把原方法的 IL 复制进这个桩方法，于是等于直接执行原版方法体，且避免"前缀替换 → 调原方法 → 又进前缀"的无限递归。
    [HarmonyPatch(typeof(RunManager), "ExitCurrentRoom")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<AbstractRoom?> OriginalExit(RunManager instance) =>
        throw new NotSupportedException("Harmony reverse patch was not installed.");
}