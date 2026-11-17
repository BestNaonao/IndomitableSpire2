using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

public sealed class VacationProposal : IndomitableRelic, IMapActiveRelic, IIndomitableClickableModel
{
    private const int GenerationFloorLimit = 45;
    private int _spentAct = -1;
    private int _proposalId;
    
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new("Uses", 1), new("ExtraOptions", 1)];
    
    // 使用单调分配且随存档保存的实例编号，而非同名 ModelId 或容易随交易/移除移动的列表下标。
    [SavedProperty]
    public int ProposalId
    {
        get => _proposalId;
        set { AssertMutable(); _proposalId = value; }
    }
    
    [SavedProperty]
    public int SpentAct
    {
        get => _spentAct;
        set { AssertMutable(); _spentAct = value; }
    }
    
    public bool HasOpportunity => IsMutable && !IsMelted && SpentAct != Owner.RunState.CurrentActIndex;
    
    // 不声明 HasUponPickupEffect：初始化编号不是一次性的拾取奖励；原版 IsTradable 自动读取 IsUsedUp。
    public override bool IsUsedUp => IsMutable && !HasOpportunity;
    
    public override bool ShowCounter => true;
    
    public override int DisplayAmount => HasOpportunity ? (int)DynamicVars["Uses"].BaseValue : 0;
    
    // 使用游戏原生逻辑兼容新的极速模式，其他模式自定义
    protected override bool IsAllowedInternal(IRunState runState) => runState.GameMode == GameMode.Blitz
        ? !IsLateInAct3(runState)
        : runState.TotalFloor < GenerationFloorLimit;
    
    public override Task AfterObtained()
    {
        VacationProposalService.EnsureIdentity(this);
        RefreshMapStatus(NMapScreen.Instance?.IsOpen == true);
        return Task.CompletedTask;
    }
    
    public override Task AfterMapGenerated(ActMap map, int actIndex)
    {
        VacationProposalService.EnsureIdentity(this);
        InvokeDisplayAmountChanged();
        RefreshMapStatus(false);
        return Task.CompletedTask;
    }
    
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        RefreshMapStatus(false);
        return Task.CompletedTask;
    }
    
    public bool SynchronizeClick => false;
    
    public bool CanHandleClickLocal(IndomitableClickContext context) =>
        context.Player == Owner && HasOpportunity && VacationMapSelectionService.CanBegin(Owner.RunState);
    
    // 这里只打开/取消本机预览。确认以后才请求 VacationProposalAction，不能把 UI 等待放进行动队列。
    public Task OnClick(PlayerChoiceContext? choiceContext, IndomitableClickContext context)
    {
        VacationMapSelectionService.Toggle(this);
        return Task.CompletedTask;
    }
    
    public override bool ShouldDisableRemainingRestSiteOptions(Player player)
    {
        if (player != Owner || !VacationProposalService.GrantsExtraChoice(this)) return true;
        Flash();
        return false;
    }
    
    internal void Spend()
    {
        SpentAct = Owner.RunState.CurrentActIndex;
        InvokeDisplayAmountChanged();
        RefreshMapStatus(NMapScreen.Instance?.IsOpen == true);
        Flash();
    }
    
    public void RefreshMapStatus(bool isMapOpen) =>
        Status = HasOpportunity && isMapOpen ? RelicStatus.Active : RelicStatus.Normal;
}