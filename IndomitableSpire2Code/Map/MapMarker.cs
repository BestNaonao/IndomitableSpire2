using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Map;

/// <summary>标记的只读 ViewModel。服务拥有存档事实，ViewModel 提供展示数据，Godot View 统一负责布局。</summary>
public abstract class MapMarker(IRunState run, MapPoint point)
{
    protected IRunState Run { get; } = run;
    
    protected MapPoint Point { get; } = point;
    
    public abstract string Id { get; }
    
    public abstract string TexturePath { get; }
    
    public abstract bool Visible { get; }
    
    public virtual bool CanPreview => true;
    
    public virtual string? Description => null;
    
    public virtual MonsterModel[] PreviewMonsters() => [];
}