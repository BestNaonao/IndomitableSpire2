using System.Runtime.CompilerServices;
using System.Text.Json;
using BaseLib.Patches.Saves;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Map;
using IndomitableSpire2.IndomitableSpire2Code.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Services;

/// <summary>共享的特殊休息处事实。UI 不写这里；只有已同步的确认行动可以提交变更。</summary>
public static class VacationProposalService
{
    public sealed class State
    {
        public int NextId { get; set; } = 1; // 用于为遗物分配单调递增的 ID
    }
    
    private static readonly ConditionalWeakTable<IRunState, State> States = new();
    
    public static event Action<IRunState, MapCoord, int, bool>? ProposalResolved; // 用于提交后通知 UI 层更新。
    
    public static void RegisterSave() => ExtendedSaveTypes.RegisterSavedValue<IRunState, string>(
        "Indomitable.VacationProposal.v1",
        run => States.TryGetValue(run, out var state) ? JsonSerializer.Serialize(state) : null,
        (run, json) =>
        {
            States.Remove(run);
            if (!string.IsNullOrEmpty(json)) States.Add(run, JsonSerializer.Deserialize<State>(json)!);
        }, 
        (json, writer) => writer.WriteString(json), reader => reader.ReadString());
    
    /// <summary>遗物身份分配，防止 ModelId 冲突。</summary>
    public static void EnsureIdentity(VacationProposal relic)
    {
        var state = States.GetOrCreateValue(relic.Owner.RunState);
        // 兼容恢复时先读取遗物、后读取扩展存档的顺序，也避开已经在其他玩家身上的编号。
        state.NextId = Math.Max(state.NextId, relic.Owner.RunState.Players.SelectMany(p => p.Relics)
            .OfType<VacationProposal>()
            .Select(r => r.ProposalId)
            .DefaultIfEmpty().Max() + 1);
        if (relic.ProposalId == 0) relic.ProposalId = state.NextId++;
    }
    
    /// <summary>节点是否有资格被选做目标。业务规则：楼层数大于当前楼层、符合类型、未访问过，并且存在于地图中。</summary>
    public static bool IsEligible(IRunState run, MapPoint point) =>
        point.coord.row > (run.CurrentMapCoord?.row ?? -1) &&
        point.PointType is MapPointType.Monster or MapPointType.Elite or MapPointType.Unknown &&
        (run is not RunState actual || !actual.VisitedMapCoords.Contains(point.coord)) &&
        MapCommand.Points(run.Map).Any(p => ReferenceEquals(p, point));
    
    /// <summary>
    /// 行动队列已确定各端顺序；检查、改节点、标记、花费之间不 await，形成一个不可分割的提交。
    /// 第一个成功者将类型改成 RestSite，后续同节点请求自然不再符合候选，不消费第二份机会。
    /// </summary>
    public static bool TryCommit(Player player, int proposalId, int act, MapCoord? source, MapCoord destination)
    {
        var run = player.RunState;
        var relic = player.Relics.OfType<VacationProposal>().SingleOrDefault(r => r.ProposalId == proposalId);
        var point = MapCommand.Points(run.Map).FirstOrDefault(p => p.coord == destination);
        var accepted = proposalId > 0 && act == run.CurrentActIndex && source == run.CurrentMapCoord &&
            relic is { HasOpportunity: true } && point != null && IsEligible(run, point);
        if (accepted)
        {
            var changed = MapMarkerService.SetPointType(run, point!, MapPointType.RestSite);
            accepted = MapCommand.Mark(new VacationProposalMapMarker(run, point!), notifyViews: false);
            if (accepted) relic!.Spend();
            if (changed || accepted) MapMarkerService.NotifyChanged(run); // 两次设置共享一次 UI 更新。
        }
        if (ProposalResolved == null) return accepted;
        foreach (var @delegate in ProposalResolved.GetInvocationList())
        {
            var listener = (Action<IRunState, MapCoord, int, bool>)@delegate;
            try { listener(run, destination, proposalId, accepted); }
            catch (Exception exception) { MainFile.Logger.Warn($"Vacation proposal view refresh failed: {exception}"); }
        }
        return accepted;
    }
    
    /// <summary>遗物能否提供额外选项的逻辑。满足前置条件后，通过该休息处选择的选项数量和遗物的索引相比较，决定是否可以提供额外选项。</summary>
    public static bool GrantsExtraChoice(VacationProposal relic)
    {
        var run = relic.Owner.RunState;
        if (relic.IsMelted || run.CurrentRoom is not RestSiteRoom || run.CurrentMapPoint is not { } point ||
            !MapMarkerService.Contains(run, point, VacationProposalMapMarker.MarkerId)) return false;
        // 原版 ChooseOption 先记录成功选择，再调用 ShouldDisableRemainingRestSiteOptions。
        // 取消锻造等失败选择不写历史，也不会消耗额外次数。Hook 遇到 false 就短路，因此不在钩子里递减共享计数。
        var chosen = run.CurrentMapPointHistoryEntry?.GetEntry(relic.Owner.NetId).RestSiteChoices.Count ?? 0;
        var copies = relic.Owner.Relics.OfType<VacationProposal>().Where(r => !r.IsMelted).ToList();
        // 第 1 次选择由第 1 份计划保留选项，第 2 次由第 2 份……使用机会是否花掉与本处额外选择无关。
        return chosen > 0 && chosen == copies.IndexOf(relic) + 1;
    }
}