using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Multiplayer;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class LuckyBag : IndomitableSpire2Relic
{
    private MerchantRoom? _merchantRoom;    // 当前商店
    private int _goldSpentBeforeMerchant;   
    private int _goldSpent;                 
    private bool _triggered;                // 是否触发过
    
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(168)];
    public override bool ShowCounter => _merchantRoom != null && !_triggered;
    public override int DisplayAmount => _goldSpent;
    
    private int? RecordedGoldSpent => Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(Owner.NetId).GoldSpent;
    
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        // 打开/关闭商店面板不会换房间；同一房间的重复通知也不能刷新次数。
        if (room != _merchantRoom) ResetMerchant(room as MerchantRoom, RecordedGoldSpent ?? 0);
        return Task.CompletedTask;
    }
    
    private void ResetMerchant(MerchantRoom? room, int goldSpentBeforeMerchant)
    {
        AssertMutable();
        _merchantRoom = room;
        _goldSpentBeforeMerchant = goldSpentBeforeMerchant;
        _goldSpent = 0;
        _triggered = false;
        Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }
    
    public override async Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        // 假商人也使用 MerchantEntry，但它所在的是 EventRoom。
        // 商店交易仅在本机处理；获得遗物的结果交给原版 RewardSynchronizer 同步。
        if (player != Owner || !LocalContext.IsMe(Owner) || Owner.RunState.CurrentRoom is not MerchantRoom room) return;
        // 在店内刚买到福袋时没有收到进房钩子；包括本次购买在内的本店消费仍然有效。
        if (room != _merchantRoom) ResetMerchant(room, 0);
        if (_triggered) return;
        
        // 买到会员卡时，原版 goldSpent 会在获得遗物之后再次读取折后价格。
        // LoseGold 在实际付款时写入的 GoldSpent 才是准确账本，也不会被返金干扰。
        _goldSpent = RecordedGoldSpent is { } recorded
            ? Math.Max(0, recorded - _goldSpentBeforeMerchant)
            : _goldSpent + Math.Max(0, goldSpent);
        InvokeDisplayAmountChanged();
        if (_goldSpent <= DynamicVars.Gold.IntValue || 
            room.Inventories.FirstOrDefault(i => i.Player == Owner) is not { } inventory) return;
        
        // 在第一次 await 前锁定：免费领取也会再次调用 AfterItemPurchased。
        _triggered = true;
        Flash();
        Status = RelicStatus.Disabled;
        InvokeDisplayAmountChanged();
        
        var rule = await LuckyBagRuleSynchronizer.GetRule();
        // 在等待房主响应期间，可能已离店；不能把旧商店的赠品发到下一个房间。
        if (Owner.RunState.CurrentRoom != room || Owner.Creature.IsDead) return;
        var stockedRelics = inventory.RelicEntries.Where(e => e.IsStocked).ToList();
        if (stockedRelics.Count > 0)
        {
            var entry = stockedRelics[Owner.PlayerRng.Shops.NextInt(stockedRelics.Count)];
            await CustomMerchantCmd.GiveRelic(inventory, entry, rule);
            return;
        }
        
        await CustomRelicCmd.GiveFromPool(Owner, rule);
    }
}