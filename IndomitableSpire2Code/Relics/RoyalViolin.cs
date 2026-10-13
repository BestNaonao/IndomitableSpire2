using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

/// <summary>休息后，直到下一个休息处为止，每回合的正常起手抽牌额外增加两张。</summary>
public sealed class RoyalViolin : IndomitableRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    
    private int _restedAtFloor = -1;
    
    /// <summary>
    /// 参考古茶具套装用 SavedProperty 保存效果，但额外记录楼层，使同一休息处的重复进房通知或读档不会误清除刚获得的增益。
    /// -1 表示未激活；其他值记录激活时的总楼层。Status 只是本机界面的展示状态，不用它判断效果，也不把它作为存档状态。
    /// </summary>
    [SavedProperty]
    public int RestedAtFloor
    {
        get => _restedAtFloor;
        set
        {
            AssertMutable();
            _restedAtFloor = value;
        }
    }
    
    public bool IsMelodyActive => RestedAtFloor >= 0;
    
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        // 参考“古茶具套装”判定休息处。到达“下一处”就失效，不等玩家选择休息/锻造；本处后续选项和打开/关闭地图都不能取消刚激活的效果。
        if (room is RestSiteRoom && RestedAtFloor != Owner.RunState.TotalFloor)
            RestedAtFloor = -1;
        // 进入任何房间时停止持续脉冲；实际抽牌增益只在下一个休息处清除。
        RefreshMapStatus(false);
        return Task.CompletedTask;
    }
    
    public override Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        // 参考“石炉加湿器”。这个钩子在执行休息后触发，即使已满血。微型帐篷仅保留其余选项，先休息后锻造和先锻造后休息都能保持激活。
        // 本遗物要求实际在休息处休息，因此不接受事件等来源的模拟休息，也不响应队友。
        if (player != Owner || isMimicked || Owner.RunState.CurrentRoom is not RestSiteRoom) return Task.CompletedTask;
        RestedAtFloor = Owner.RunState.TotalFloor;
        Flash();
        RefreshMapStatus(NMapScreen.Instance?.IsOpen == true);
        return Task.CompletedTask;
    }
    
    // 参考参考原版“小提琴”，沿用原版起手抽牌、先天牌和手牌上限的处理，也不会被小提琴的回合中禁抽误挡。
    public override decimal ModifyHandDraw(Player player, decimal count) => 
        player == Owner && IsMelodyActive ? count + DynamicVars.Cards.BaseValue : count;
    
    // 参考“怀表”，每次生效短暂闪烁；不设置 Active，避免战斗内持续闪烁。
    public override Task AfterModifyingHandDraw()
    {
        Flash();
        return Task.CompletedTask;
    }
    
    // 参考“石炉加湿器”：将效果提示追加到自己的“休息”选项，保留其他遗物的提示。
    public override IReadOnlyList<LocString> ModifyExtraRestSiteHealText(
        Player player, IReadOnlyList<LocString> currentExtraText) => 
        player == Owner && AdditionalRestSiteHealText is { } text ? [..currentExtraText, text] : currentExtraText;
    
    /// <summary>
    /// 由 RoyalViolinMapStatusPatch 在地图打开/关闭后刷新。IsOpen 区分地图和房间界面；不能用 CurrentRoom 判断当前显示的界面。
    /// 这是纯展示状态，各端可独立切换地图而不影响已经同步、可保存的 RestedAtFloor。
    /// </summary>
    internal void RefreshMapStatus(bool isMapOpen)
    {
        Status = IsMelodyActive && !IsMelted && isMapOpen ? RelicStatus.Active : RelicStatus.Normal;
    }
}