using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

/// <summary>同步逻辑使用的地图命令；不打开 UI，不发送预览消息，不改既有历史或地图连边。</summary>
public static class MapCommand
{
    // 安全地收集地图上所有有效节点，包括可能存在的第二个 Boss 节点，并使用 DistinctBy 去重。
    public static IEnumerable<MapPoint> Points(ActMap map) => map.GetAllMapPoints()
        .Append(map.StartingMapPoint).Append(map.BossMapPoint)
        .Concat(map.SecondBossMapPoint is { } second ? [second] : [])
        .DistinctBy(p => p.coord);
    
    public static void SetPointType(IRunState run, MapPoint point, MapPointType type, bool notifyViews = true)
    {
        point.PointType = type;
        EyeInTheSkyService.ForgetEncounter(run, point);
        if (notifyViews) MapMarkerService.NotifyChanged(run);
    }
    
    public static void MakeVacationSite(IRunState run, MapPoint point, bool notifyViews = true)
    {
        VacationProposalService.MarkSite(run, point);
        SetPointType(run, point, MapPointType.RestSite, notifyViews);
    }
    
    public static void MarkEyeInTheSky(IRunState run, ActMap map, MapPoint point)
    {
        EyeInTheSkyService.AddMark(run, map, point);
        MapMarkerService.NotifyChanged(run);
    }
}