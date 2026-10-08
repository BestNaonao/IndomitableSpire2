using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class CustomMerchantCmd
{
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
            if (!await entry.OnTryPurchaseWrapper(inventory, ignoreCost: true))
                await CustomRelicCmd.GiveFromPool(inventory.Player, rule);
            return;
        }
        if (entry.Model is not { } relic) return;
        await CustomRelicCmd.OfferGift(relic, inventory.Player, rule, fromPool: false,
            claim: () => entry.Model == relic   // 二次判断：执行前的最终状态校验（乐观锁）
                ? entry.OnTryPurchaseWrapper(inventory, ignoreCost: true) : Task.FromResult(false),
            discard: () => DiscardRelic(inventory, entry, relic));
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