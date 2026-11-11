using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Map;

/// <summary>展示投影，不持有 UI，也不修改标记/遭遇。工厂根据提示数据选择不同的 View。</summary>
public abstract class MapMarkerViewModel(MapMarker marker)
{
    public MapMarker Marker { get; } = marker;
    
    public string Id => Marker.Id;
    
    public abstract string TexturePath { get; }
    
    public virtual bool Visible => true;
    
    public virtual bool CanPreview => true;
    
    public virtual MapMarkerHoverContent? CreateHoverContent() => null;
}

/// <summary>标记悬浮展示内容</summary>
public abstract record MapMarkerHoverContent;

public sealed record MapMarkerTips(IReadOnlyList<IHoverTip> Tips) : MapMarkerHoverContent;

public sealed record MapMarkerEnemies(IReadOnlyList<MonsterModel> Monsters) : MapMarkerHoverContent;