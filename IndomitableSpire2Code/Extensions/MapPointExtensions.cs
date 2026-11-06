using MegaCrit.Sts2.Core.Map;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class MapPointExtensions
{
    /// <summary>判断该地图节点类型是否为战斗遭遇（普通、精英或 Boss）。</summary>
    public static bool IsCombat(this MapPointType type) =>
        type is MapPointType.Monster or MapPointType.Elite or MapPointType.Boss;
    
    /// <summary>根据类型判断该地图节点是否为战斗遭遇节点。</summary>
    public static bool IsCombatPoint(this MapPoint point) => point.PointType.IsCombat();
}