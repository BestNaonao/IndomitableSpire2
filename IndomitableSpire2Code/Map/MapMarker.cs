using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Map;

/// <summary>
/// 一个节点上的一种标记模型。服务保存归属关系；模型拥有规则和生命周期钩子。不保存 Godot 节点，UI 通过独立 ViewModel 读取展示信息。
/// 构造/读档只重建模型，不执行 OnAdded；只有同步游戏逻辑中的 MapCommand.Mark 才执行它。
/// </summary>
public abstract class MapMarker(IRunState run, MapPoint point)
{
    public IRunState Run { get; } = run;
    
    public MapPoint Point { get; } = point;
    
    public int Act { get; } = run.CurrentActIndex;
    
    public abstract string Id { get; }
    
    /// <summary>类型规则用于初次附着和后续保留。默认允许任意节点类型。</summary>
    public virtual bool CanMarkPointType(MapPointType type) => true;
    
    /// <summary>
    /// 判断标记能否附着于当前节点，应无副作用。地图恢复或节点类型改变时也用此条件判断能否保留。
    /// </summary>
    public virtual bool CanMark() => CanMarkPointType(Point.PointType);
    
    public abstract MapMarkerViewModel CreateViewModel();
    
    public virtual void OnAdded() { }
    
    /// <summary>撤销模型自身的效果，业务中应包括它在 OnAdded 中创建并负责管理的依赖标记。</summary>
    public virtual void OnRemoved() { }
    
    public virtual void OnPointTypeChanged(MapPointType previous, MapPointType current) { }
}