using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

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
    
    // 只允许在第三阶段的宝箱房之前出现
    protected override bool IsAllowedInternal(IRunState runState) => !IsLateInAct3(runState);
    
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
        // 0.113 的付费交易在各端重放；赠品也必须在同一购买任务内完整结算。
        if (player != Owner || Owner.RunState.CurrentRoom is not MerchantRoom room) return;
        // 在店内刚买到福袋时没有收到进房钩子；包括本次购买在内的本店消费仍然有效。
        if (room != _merchantRoom) ResetMerchant(room, 0);
        if (_triggered) return;
        
        // 买到会员卡时，原版 goldSpent 会在获得遗物之后再次读取折后价格。
        // LoseGold 在实际付款时写入的 GoldSpent 才是准确账本，也不会被返金干扰。
        _goldSpent = RecordedGoldSpent is { } recorded
            ? Math.Max(0, recorded - _goldSpentBeforeMerchant)
            : _goldSpent + Math.Max(0, goldSpent);
        InvokeDisplayAmountChanged();
        if (_goldSpent <= DynamicVars.Gold.IntValue) return;
        var inventory = RunManager.Instance.MerchantSynchronizer.GetInventoryFor(Owner);
        
        // 在第一次 await 前锁定：免费领取也会再次调用 AfterItemPurchased。
        _triggered = true;
        Flash();
        Status = RelicStatus.Disabled;
        InvokeDisplayAmountChanged();
        
        await LuckyBagGiftSynchronizer.Give(inventory, room);
    }
}