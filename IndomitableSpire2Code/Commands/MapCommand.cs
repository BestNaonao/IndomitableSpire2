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
        marker.OnAdded();
        if (notifyViews) MapMarkerService.NotifyChanged(marker.Run);
        return true;
    }
    
    public static bool Remove(MapMarker marker, bool notifyViews = true)
    {
        if (!MapMarkerService.Remove(marker)) return false;
        marker.OnRemoved();
        if (notifyViews) MapMarkerService.NotifyChanged(marker.Run);
        return true;
    }
    
    public static void SetPointType(IRunState run, MapPoint point, MapPointType type, bool notifyViews = true)
    {
        if (MapMarkerService.SetPointType(run, point, type) && notifyViews) MapMarkerService.NotifyChanged(run);
    }
    
    /// <summary>只取旧源集合的一步邻居；先去重、按行列排序，再抽签，不沿本次新增节点继续扩散。</summary>
    public static MapPoint[] Adjacent(IEnumerable<MapPoint> sources, Func<MapPoint, bool> eligible) =>
        sources.SelectMany(p => p.parents.Concat(p.Children)).Where(eligible).DistinctBy(p => p.coord)
            .OrderBy(p => p.coord.row)
            .ThenBy(p => p.coord.col).ToArray();
    
    public static IReadOnlyList<T> Spread<T>(IRunState run, IEnumerable<MapPoint> sources, int limit, Rng rng,
        Func<MapPoint, bool> eligible, Func<IRunState, MapPoint, T> create) where T : MapMarker
    {
        var candidates = Adjacent(sources, eligible).ToList();
        var added = new List<T>();
        while (added.Count < limit && candidates.Count > 0)
        {
            var index = rng.NextInt(candidates.Count);
            var point = candidates[index]; candidates.RemoveAt(index);
            var marker = create(run, point);
            if (Mark(marker, notifyViews: false)) added.Add(marker);
        }
        if (added.Count > 0) MapMarkerService.NotifyChanged(run);
        return added;
    }
    
    // 已有调用的便利入口；业务规则由 Marker.OnAdded 实现。
    public static bool MakeVacationSite(IRunState run, MapPoint point, bool notifyViews = true) =>
        Mark(new VacationProposalMapMarker(run, point), notifyViews);
    
    public static void MarkEyeInTheSky(IRunState run, ActMap map, MapPoint point) =>
        Mark(new EyeInTheSkyMapMarker(run, point, map));
}