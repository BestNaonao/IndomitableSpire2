using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class CustomMerchantCmd
{
    // 标记当前是本 Mod 发起的赠品领取
    private static readonly AsyncLocal<MerchantRelicEntry?> ActiveClaim = new();
    
    internal static bool IsGiftClaim(MerchantRelicEntry entry) => ActiveClaim.Value == entry;
    
    private static readonly Action<MerchantRelicEntry, MerchantInventory?> Restock =
        AccessTools.MethodDelegate<Action<MerchantRelicEntry, MerchantInventory?>>(
            AccessTools.DeclaredMethod(typeof(MerchantRelicEntry), "RestockAfterPurchase"));
    private static readonly Action<MerchantRelicEntry> Clear =
        AccessTools.MethodDelegate<Action<MerchantRelicEntry>>(
            AccessTools.DeclaredMethod(typeof(MerchantRelicEntry), "ClearAfterPurchase"));
    
    /// <summary>领取沿用原版免费购买；跳过放回不动货位，跳过销毁仍支持送货员补货。</summary>
    public static async Task GiveRelic(MerchantInventory inventory, MerchantRelicEntry entry, LuckyBagRelicRule rule)
    {
        if (rule == LuckyBagRelicRule.Direct)
        {
            if (!await ClaimRelic(inventory, entry))
                await CustomRelicCmd.GiveFromPool(inventory.Player, rule);
            return;
        }
        if (entry.Model is not { } relic) return;
        await CustomRelicCmd.OfferGift(relic, inventory.Player, rule, fromPool: false,
            claim: () => entry.Model == relic   // 二次判断：执行前的最终状态校验（乐观锁）
                ? ClaimRelic(inventory, entry) : Task.FromResult(false),
            discard: () => DiscardRelic(inventory, entry, relic));
    }
    
    private static async Task<bool> ClaimRelic(MerchantInventory inventory, MerchantRelicEntry entry)
    {
        // 普通交易由新版 MerchantSynchronizer 同步。赠品保留旧版的本地免费交易与奖励消息，
        // 由 MerchantGiftSyncPatch 在补货、购买后钩子之前同步入包，保持原来的执行次序。
        var previous = ActiveClaim.Value;
        ActiveClaim.Value = entry;
        try { return await entry.OnTryPurchaseUnsynchronized(inventory, ignoreCost: true); }
        finally { ActiveClaim.Value = previous; }
    }
    
    /// <summary>
    /// 相当于不把遗物交给玩家的免费交易。先记录和消耗池，再按原版顺序进行补货或清空、
    /// 广播零消费购买钩子、刷新商品 UI；不发送“获得遗物”或扣金消息。
    /// </summary>
    public static async Task DiscardRelic(MerchantInventory inventory, MerchantRelicEntry entry, RelicModel relic)
    {
        if (entry.Model != relic) return;
        var player = inventory.Player;
        player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).BoughtRelics.Add(relic.Id); // 计入历史
        CustomRelicCmd.Discard(relic, player);  // 自定义丢弃
        if (player.RunState.CurrentRoom is MerchantRoom && Hook.ShouldRefillMerchantEntry(player.RunState, entry, player))
            Restock(entry, inventory);          // 原版补货
        else Clear(entry);                      // 原版清理商店位置
        await Hook.AfterItemPurchased(player.RunState, player, entry, 0);   // 原版钩子
        entry.InvokePurchaseCompleted(entry);   // 原版结束
    }
}