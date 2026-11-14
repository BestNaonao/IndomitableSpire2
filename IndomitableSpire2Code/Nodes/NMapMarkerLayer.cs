using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Map;
using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>每个地图节点一个布局控制器；原版 QuestIcon 优先占位，自定义标记随后按稳定 ID 排序。</summary>
public partial class NMapMarkerLayer : Node
{
    private const string NodeName = "CustomMarkerLayout";
    private NMapPoint _point = null!; // 所属的原版地图点节点
    private Control _parent = null!;
    private IRunState _run = null!;
    
    private readonly Dictionary<string, NCustomMapMarker> _views = new(StringComparer.Ordinal); // 实际状态的 NMarkers
    
    // 幂等挂载到 %IconContainer 或地图点节点本身。
    public static void Attach(NMapPoint point)
    {
        var parent = point.GetNodeOrNull<Control>("%IconContainer") ?? point;
        if (parent.GetNodeOrNull(NodeName) != null) return;
        var layer = new NMapMarkerLayer
        {
            Name = NodeName, _point = point, _parent = parent,
            _run = (IRunState)AccessTools.Field(typeof(NMapPoint), "_runState").GetValue(point)!
        };
        parent.AddChild(layer);
        layer.Refresh();
    }
    
    public override void _EnterTree() => MapMarkerService.Changed += OnChanged;
    
    public override void _ExitTree() => MapMarkerService.Changed -= OnChanged;
    
    public override void _Process(double delta) => Refresh();
    
    private void OnChanged(IRunState run)
    {
        if (_run != run) return;
        _point.RefreshVisualsInstantly();
        foreach (var view in _views.Values) view.ClosePreview();
        Refresh();
    }
    
    // 核心刷新逻辑
    private void Refresh()
    {
        var models = MapMarkerService.GetMarkers(_run, _point.Point); // 期望状态
        // 找出当前视图中有，而期望状态中没有的模型 ID，然后移除对应视图。
        foreach (var id in _views.Keys.Where(id => models.All(m => m.Id != id)).ToArray())
        {
            var old = _views[id];
            _views.Remove(id);
            old.ClosePreview();
            old.Hide();
            old.QueueFree();
        }
        // 对于当前视图中没有，或引用模型不同的，需要移除旧实例（如果有）再重建实例。
        foreach (var model in models)
        {
            if (_views.TryGetValue(model.Id, out var existing) && existing.Model.Marker == model) continue;
            if (existing != null)
            {
                existing.ClosePreview();
                existing.Hide();
                existing.QueueFree();
            }
            var view = NCustomMapMarker.Create(model.CreateViewModel());
            _views[model.Id] = view;
            _parent.AddChild(view);
        }
        // 标记排布，以父节点为中心，原版标记存在时占用一个槽位。剩余模型按照 MapMarkerLayout 规则排布，不可见的模型不占用槽位。
        var center = _parent.Size / 2f;
        var slot = 0;
        var quest = _point.GetNodeOrNull<Control>("%QuestIcon");
        if (quest != null && _point.Point.Quests.Count > 0)
        {
            // 判断依据是模型侧数据，Quests 可有多项，但原版只绘制一个 QuestIcon，故只占一个槽位；保留原版缩放动画。
            quest.Position = center + MapMarkerLayout.Offset(slot++)!.Value - quest.Size / 2f;
        }
        foreach (var model in models)
        {
            var view = _views[model.Id];
            var offset = view.Model.Visible ? MapMarkerLayout.Offset(slot++) : null; // 可见时消耗槽位
            view.Visible = offset.HasValue;
            if (offset is { } value) view.Position = center + value - view.Size / 2f;
            else view.ClosePreview(); // 超过 16 个只截断显示，不删除模型/效果/存档。
        }
    }
}