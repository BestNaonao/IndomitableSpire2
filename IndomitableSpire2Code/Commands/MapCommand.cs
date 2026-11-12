using IndomitableSpire2.IndomitableSpire2Code.Map;
using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

/// <summary>同步模型的控制入口；不发送 UI 消息，不修改既有历史/连边。调用者负责同步执行顺序。</summary>
public static class MapCommand
{
    // 安全地收集地图上所有有效节点，包括可能存在的第二个 Boss 节点，并使用 DistinctBy 去重。
    public static IEnumerable<MapPoint> Points(ActMap map) => map.GetAllMapPoints()
        .Append(map.StartingMapPoint).Append(map.BossMapPoint)
        .Concat(map.SecondBossMapPoint is { } second ? [second] : [])
        .DistinctBy(p => p.coord);
    
    public static bool Mark(MapMarker marker, bool notifyViews = true)
    {
        if (!MapMarkerService.Add(marker)) return false;
        try { marker.OnAdded(); }
        catch
        {
            Remove(marker, notifyViews);
            throw;
        }
        if (notifyViews) MapMarkerService.NotifyChanged(marker.Run);
        return MapMarkerService.Contains(marker.Run, marker.Point, marker.Id);
    }
    
    public static bool Remove(MapMarker marker, bool notifyViews = true)
    {
        if (!MapMarkerService.Remove(marker)) return false;
        marker.OnRemoved();
        if (notifyViews) MapMarkerService.NotifyChanged(marker.Run);
        return true;
    }
    
    // 包装设置节点方法，并且可以设置是否通知 UI 更新。
    public static void SetPointType(IRunState run, MapPoint point, MapPointType type, bool notifyViews = true)
    {
        if (MapMarkerService.SetPointType(run, point, type) && notifyViews) MapMarkerService.NotifyChanged(run);
    }
    
    /// <summary>只取旧源集合的一步邻居；先去重、筛选、按行列排序，再抽签，不沿本次新增节点继续扩散。</summary>
    public static MapPoint[] Adjacent(IEnumerable<MapPoint> sources, Func<MapPoint, bool> eligible) => sources
        .SelectMany(p => p.parents.Concat(p.Children))
        .DistinctBy(p => p.coord).Where(eligible)
        .OrderBy(p => p.coord.row).ThenBy(p => p.coord.col)
        .ToArray();
    
    public static IReadOnlyList<T> Spread<T>(IRunState run, IEnumerable<MapPoint> sources, int limit, Rng rng,
        Func<IRunState, MapPoint, T> create) where T : MapMarker =>
        Spread(run, sources, limit, rng, _ => true, create);
    
    public static IReadOnlyList<T> Spread<T>(IRunState run, IEnumerable<MapPoint> sources, int limit, Rng rng,
        Func<MapPoint, bool> eligible, Func<IRunState, MapPoint, T> create) where T : MapMarker
    {
        var markedSources = sources.DistinctBy(p => p.coord)
            .Where(source => MapMarkerService.Contains(run, source, create(run, source).Id));
        // LINQ 表达式，邻接节点中创建暂时标记，并筛选可标记和没有数据记录的。
        var candidates = (from point in Adjacent(markedSources, eligible)
            let marker = create(run, point)
            where marker.CanMark() && !MapMarkerService.Contains(run, point, marker.Id)
            select marker).ToList();
        // 抽签前排除非法类型和同名标记，保持原有的确定性顺序与随机数消耗。
        var added = new List<T>();
        while (added.Count < limit && candidates.Count > 0)
        {
            var index = rng.NextInt(candidates.Count);
            var marker = candidates[index];
            candidates.RemoveAt(index);
            if (Mark(marker, notifyViews: false)) added.Add(marker);
        }
        if (added.Count > 0) MapMarkerService.NotifyChanged(run);
        return added;
    }
}