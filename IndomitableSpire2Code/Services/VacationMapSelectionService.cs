using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Actions;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using IndomitableSpire2.IndomitableSpire2Code.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Services;

/// <summary>
/// 纯本地的地图选择会话服务；管理本机的地图选择状态机，不写地图、不消费次数，也不通过网络同步鼠标、弹窗或选中状态。
/// 功能包括维护当前会话、控制底部提示文本的显示与隐藏、处理地图点的预选和确认、在确认后向全局行动队列提交网络同步请求、监听事件和处理多人游戏冲突等。
/// </summary>
public static class VacationMapSelectionService
{
    // 不可变会话数据快照，包含遗物实例、阶段数、当前坐标和地图生成代数
    private sealed record Session(VacationProposal Relic, int Act, MapCoord? Source, int Generation);
    private static readonly string LocEntryKeyPrefix = ModelDb.Relic<VacationProposal>().Id.Entry;
    private static Session? _session; // 本地的当前会话。
    private static VacationConfirmation? _confirmation; // 确认弹窗的 UI 节点
    private static MapCoord? _pending; // 玩家点击但未确认的暂存坐标
    private static bool _submitted; // 防重入标志，一旦向网络队列提交请求就设置为 true
    private static Label? _hint; // 屏幕底部的提示文本 UI 节点
    
    public static bool IsSelecting => _session != null; // 是否处于选择模式
    public static bool HasConfirmation => GodotObject.IsInstanceValid(_confirmation); // 是否已确认
    
    static VacationMapSelectionService() => VacationProposalService.ProposalResolved += OnResolved;
    
    public static bool CanBegin(IRunState run) =>
        !_submitted && NMapScreen.Instance is { IsOpen: true, IsTraveling: false } map &&
        ActiveScreenContext.Instance.IsCurrent(map) && map.Drawings.GetLocalDrawingMode() == DrawingMode.None;
    
    /// <summary>切换选择模式，在下一个房间选择和休假计划选择间切换。</summary>
    public static void Toggle(VacationProposal relic)
    {
        if (_submitted) return; // 已确认的行动不能用本机取消撤销；等待队列结果，防止切换另一个实例重复提交。
        // 实现“点击开启，再点击关闭”的开关逻辑。第一次点击无会话，same 为 false，进入创建会话；再次点击相同遗物 same 为 true，清理后退出。
        var same = _session?.Relic == relic;
        Cancel();
        if (same || !relic.HasOpportunity || !CanBegin(relic.Owner.RunState)) return;
        // 创建会话状态快照。实例编号已在同步的拾取/生成钩子中分配；本地 UI 绝不分配编号。
        _session = new Session(relic, relic.Owner.RunState.CurrentActIndex, relic.Owner.RunState.CurrentMapCoord,
            RunManager.Instance.MapSelectionSynchronizer.MapGenerationCount);
        _hint = new Label
        {
            Name = "VacationSelectionHint",
            Text = new LocString("relics", $"{LocEntryKeyPrefix}.selection").GetFormattedText(),
            HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        NMapScreen.Instance!.AddChild(_hint);
        _hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        _hint.OffsetTop = -90; _hint.OffsetBottom = -25; _hint.OffsetLeft = 120; _hint.OffsetRight = -120;
        _hint.AddThemeFontSizeOverride("font_size", 22);
        _hint.AddThemeColorOverride("font_color", new Color("f0ce79"));
        _hint.AddThemeColorOverride("font_outline_color", Colors.Black);
        _hint.AddThemeConstantOverride("outline_size", 5);
        Refresh();
    }
    
    /// <summary>彻底取消会话</summary>
    public static void Cancel()
    {
        if (_session == null && !HasConfirmation && !GodotObject.IsInstanceValid(_hint)) return;
        _session = null;
        _submitted = false;
        if (GodotObject.IsInstanceValid(_hint)) { _hint.Hide(); _hint.QueueFree(); }
        _hint = null;
        DismissConfirmation();
    }
    
    /// <summary>关闭或取消确认弹窗</summary>
    public static void DismissConfirmation()
    {
        if (GodotObject.IsInstanceValid(_confirmation)) { _confirmation.Hide(); _confirmation.QueueFree(); }
        _confirmation = null;
        _pending = null;
        if (GodotObject.IsInstanceValid(_hint)) _hint.Show();
        Refresh();
    }
    
    // 地图点是否可以被选中。必须是未提交的会话、无确认弹窗、遗物有使用机会、满足基础 CanBegin 条件和 Service 的业务规则。
    public static bool CanSelect(MapPoint point) =>
        _session is { } session && !_submitted && !HasConfirmation && session.Relic.HasOpportunity && 
        CanBegin(session.Relic.Owner.RunState) && VacationProposalService.IsEligible(session.Relic.Owner.RunState, point);
    
    /// <summary>处理节点点击。记录未确认的节点坐标，根据配置决定是否需要弹出确认弹窗。</summary>
    public static void Select(MapPoint point)
    {
        if (!CanSelect(point)) return;
        _pending = point.coord;
        if (!IndomitableConfiguration.ConfirmVacationProposal) { Confirm(); return; }
        _hint?.Hide();
        _confirmation = VacationConfirmation.Show(NMapScreen.Instance!, Confirm, DismissConfirmation);
        Refresh();
    }
    
    /// <summary>确认并提交网络请求</summary>
    private static void Confirm()
    {
        if (_session is not { } session || _pending is not { } target || _submitted) return;
        DismissConfirmation();
        _pending = target; // 保持记录，用于 OnResolved 中比较
        _submitted = true; // 锁定状态
        if (GodotObject.IsInstanceValid(_hint))
            _hint.Text = new LocString("relics", $"{LocEntryKeyPrefix}.waiting").GetFormattedText();
        Refresh();
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new VacationProposalAction(session.Relic.Owner,
            session.Relic.ProposalId, session.Act, session.Source, target, session.Generation));
    }
    
    /// <summary>处理服务端裁决结果</summary>
    private static void OnResolved(IRunState run, MapCoord target, int proposalId, bool accepted)
    {
        if (_session is not { } session || session.Relic.Owner.RunState != run) return;
        if (session.Relic.ProposalId == proposalId) // 如果是自己的会话发起的请求
        {
            if (accepted) { Cancel(); return; } // 执行成功，彻底清理本地状态。
            _submitted = false; // 执行失败，则解锁状态，允许玩家重新提交。
            if (GodotObject.IsInstanceValid(_hint))
                _hint.Text = new LocString("relics", $"{LocEntryKeyPrefix}.selection").GetFormattedText();
            DismissConfirmation(); // 关闭可能存在的确认弹窗，让玩家知道此次操作失败。
        }
        else if (accepted && _pending == target)
        {
            // 队友抢先选择这个节点：关闭正在显示的重复确认框，保留自己这份计划与地图选择模式。
            // 已经入队的本机请求仍会在服务中被拒绝，不会扣次数。
            DismissConfirmation();
        }
        Refresh();
    }
    
    public static void ValidateSession()
    {
        if (_session is not { } session) return;
        var run = session.Relic.Owner.RunState;
        if (!session.Relic.Owner.Relics.Contains(session.Relic) || !session.Relic.HasOpportunity ||
            run.CurrentActIndex != session.Act || run.CurrentMapCoord != session.Source ||
            RunManager.Instance.MapSelectionSynchronizer.MapGenerationCount != session.Generation)
            Cancel();
    }
    
    /// <summary>
    /// 刷新每个地图节点的视觉效果。配合补丁中对 get_IsTravelable 的拦截，从而实现“符合条件的节点高亮，不符合的变暗”的视觉效果。
    /// </summary>
    private static void Refresh()
    {
        if (NMapScreen.Instance is { } map && map.IsInsideTree()) map.RefreshAllPointVisuals();
    }
}