using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Map;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>标记 View：只处理悬停和生命周期。排布交给 NMapMarkerLayer，弹窗交给可扩展工厂。</summary>
public partial class NCustomMapMarker : TextureRect
{
    public MapMarkerViewModel Model { get; private set; } = null!;
    private MapMarkerPopup? _popup;
    private bool _hovered;
    
    public static NCustomMapMarker Create(MapMarkerViewModel model) => new()
    {
        Name = model.Id + "Marker", Model = model,
        Texture = ResourceLoader.Load<Texture2D>(model.TexturePath),
        ExpandMode = ExpandModeEnum.IgnoreSize, StretchMode = StretchModeEnum.KeepAspectCentered,
        Size = Vector2.One * MapMarkerLayout.MarkerSize, MouseFilter = MouseFilterEnum.Pass
    };
    
    public override void _Ready()
    {
        MouseEntered += () => _hovered = true; // 懒创建
        MouseExited += () => { _hovered = false; ClosePreview(); }; // 急退出
    }
    
    public override void _EnterTree() => ActiveScreenContext.Instance.Updated += OnActiveScreenChanged;
    
    public override void _ExitTree()
    {
        ActiveScreenContext.Instance.Updated -= OnActiveScreenChanged;
        ClosePreview();
    }
    
    private void OnActiveScreenChanged()
    {
        if (NMapScreen.Instance is { } map && ActiveScreenContext.Instance.IsCurrent(map)) return;
        _hovered = false;
        ClosePreview();
    }
    
    public override void _Process(double delta)
    {
        var map = NMapScreen.Instance;
        if (!_hovered || !IsVisibleInTree() || map is not { IsOpen: true, IsTraveling: false } ||
            !ActiveScreenContext.Instance.IsCurrent(map) || !Model.CanPreview || NHoverTipSet.shouldBlockHoverTips)
        {
            ClosePreview();
            return;
        }
        _popup ??= MapMarkerPopupFactory.Create(this, map, Model.CreateHoverContent());
        _popup?.Reposition(this); // 跟随父节点重定位
    }
    
    public void ClosePreview()
    {
        _popup?.Dispose();
        _popup = null;
    }
}