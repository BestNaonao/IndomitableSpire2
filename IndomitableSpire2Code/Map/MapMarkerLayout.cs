using Godot;

namespace IndomitableSpire2.IndomitableSpire2Code.Map;

/// <summary>中心偏移（不是左上角）：在四个象限后接 12 个外圈槽位。Godot 的 Y 轴与数学坐标相反。</summary>
public static class MapMarkerLayout
{
    public const int Capacity = 16;
    private const int CornerSlots = 4;
    public const float MarkerSize = 48f;
    private const float CornerOffset = 28f;
    private const float OuterRadius = 108f;
    private const float AngleStep = 30f;
    
    /// <summary>优先填充四个内对角位，然后填充十二个外环位置</summary>
    public static Vector2? Offset(int slot) => slot switch
    {
        0 => new Vector2(CornerOffset, -CornerOffset),
        1 => new Vector2(-CornerOffset, -CornerOffset),
        2 => new Vector2(-CornerOffset, CornerOffset),
        3 => new Vector2(CornerOffset, CornerOffset),
        >= CornerSlots and < Capacity => OuterRadius * new Vector2(
            Mathf.Cos(Mathf.DegToRad((slot - CornerSlots) * AngleStep)),
            -Mathf.Sin(Mathf.DegToRad((slot - CornerSlots) * AngleStep))),
        _ => null
    };
}