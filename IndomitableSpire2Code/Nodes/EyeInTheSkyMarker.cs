using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Relics.Ancients;
using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>
/// 仿照 NNormalMapPoint 的 QuestIcon 放置独立标记；不占用 Quests，避免覆盖皮草大衣的骷髅。
/// 此节点只有显示功能：生成预览使用 Encounter 的独立副本，不参与地图/战斗随机序列。
/// </summary>
public partial class EyeInTheSkyMarker : TextureRect
{
    private const string ImagePath = "res://IndomitableSpire2/images/packed/map/icons/map_eye_in_the_sky_marker.png";
    private const float MarkerSize = 48f;
    private const float CellWidth = 144f;
    private const float CellHeight = 174f;
    private const float Padding = 14f;
    private NMapPoint _point = null!; // 依附的地图节点
    private IRunState _run = null!; // 游戏运行状态
    private NinePatchRect? _popup; // 弹出的预览窗口
    private bool _hovered; // 鼠标是否悬停其上
    
    public static void Attach(NMapPoint point)
    {
        // 原版皮草大衣渲染的 QuestIcon 的父节点是 IconContainer。
        var iconContainer = point.GetNodeOrNull<Control>("%IconContainer");
        var parent = iconContainer ?? point;
        if (parent.GetNodeOrNull("EyeInTheSkyMarker") != null) return;
        var marker = new EyeInTheSkyMarker
        {
            Name = "EyeInTheSkyMarker", _point = point,
            _run = (IRunState)AccessTools.Field(typeof(NMapPoint), "_runState").GetValue(point)!,
            Texture = ResourceLoader.Load<Texture2D>(ImagePath),
            ExpandMode = ExpandModeEnum.IgnoreSize, StretchMode = StretchModeEnum.KeepAspectCentered,
            // 与原版 QuestIcon 一样使用默认相对 Z=0，不能跨过地图上方的暂停/设置遮罩。
            Size = Vector2.One * MarkerSize, MouseFilter = MouseFilterEnum.Pass
        };
        // 普通节点的 QuestIcon 位于 56px 图标容器右上方。Boss/先古无该容器，使用节点中心。
        parent.AddChild(marker);
        marker.Position = iconContainer != null ? new Vector2(32, -24)
            : point.Size / 2f + new Vector2(4, -52);
        marker.MouseEntered += () => marker._hovered = true;
        marker.MouseExited += () => { marker._hovered = false; marker.ClosePreview(); };
        marker.Refresh();
    }
    
    public override void _Process(double delta) => Refresh();
    
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
    
    private void Refresh()
    {
        Visible = EyeInTheSkyService.IsMarked(_run, _point.Point);
        // 与皮草大衣同时标记时移到左上角，保留两个独立图标。
        if (_point is NNormalMapPoint)
            Position = new Vector2(_point.Point.Quests.Count > 0 ? -24 : 32, -24);
        var allowed = IsVisibleInTree() && NMapScreen.Instance is { IsOpen: true, IsTraveling: false } map &&
            ActiveScreenContext.Instance.IsCurrent(map) &&
            LocalContext.GetMe(_run.Players)?.Relics.OfType<EyeInTheSky>().Any() == true;
        if (!allowed || !_hovered)
        {
            ClosePreview();
            return;
        }
        if (_popup == null) OpenPreview(EyeInTheSkyService.PreviewMonsters(_run, _point.Point));
        if (_popup == null) return;
        // 预览弹窗位置右偏，计算最大边界防止超出屏幕右侧和底部，并使用 Clamp 将位置限制在安全区域内。
        var position = GetGlobalTransformWithCanvas().Origin + new Vector2(MarkerSize + Padding, 0);
        var limit = GetViewportRect().Size - _popup.Size - Vector2.One * Padding;
        var clamped = position.Clamp(Vector2.One * Padding, limit.Max(Vector2.One * Padding));
        // 将全局坐标转换回弹窗父节点的局部坐标（AffineInverse），确保无论 UI 层级如何嵌套，弹窗都能准确渲染在正确位置。
        _popup.Position = ((CanvasItem)_popup.GetParent()).GetGlobalTransformWithCanvas().AffineInverse() * clamped;
    }
    
    /// <summary>行列尽量接近正方形，并使 rows + columns 最小。</summary>
    public static (int Columns, int Rows) GridSize(int count)
    {
        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));
        return (columns, Math.Max(1, (count + columns - 1) / columns));
    }
    
    private void OpenPreview(MonsterModel[] monsters)
    {
        if (monsters.Length == 0) return;
        var (columns, rows) = GridSize(monsters.Length);
        _popup = new NinePatchRect
        {
            Name = "EnemyPreview",
            Texture = ResourceLoader.Load<Texture2D>("res://images/ui/hover_tip.png"),
            SelfModulate = new Color(0.08f, 0.08f, 0.08f, 0.90f),
            PatchMarginLeft = 12, PatchMarginRight = 12, PatchMarginTop = 12, PatchMarginBottom = 12,
            Size = new Vector2(columns * CellWidth + Padding * 2, rows * CellHeight + Padding * 2),
            MouseFilter = MouseFilterEnum.Ignore
        };
        // 优先挂载到 NMapScreen 实例的末尾上，确保它在地图层级渲染，不会穿透到全局菜单之上。
        ((Node?)NMapScreen.Instance ?? this).AddChild(_popup);
        for (var i = 0; i < monsters.Length; i++)
        {
            var r = Math.DivRem(i, columns, out var c);
            var cell = new Control
            {
                Position = new Vector2(Padding + c * CellWidth, Padding + r * CellHeight),
                Size = new Vector2(CellWidth, CellHeight),
                MouseFilter = MouseFilterEnum.Ignore
            };
            _popup.AddChild(cell);
            EyeInTheSkyPortrait.Add(cell, monsters[i], CellWidth);
            var label = new Label
            {
                Text = monsters[i].Title.GetFormattedText(),
                Position = new Vector2(0, CellWidth),
                Size = new Vector2(CellWidth, CellHeight - CellWidth),
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore
            };
            label.AddThemeFontSizeOverride("font_size", 16);
            cell.AddChild(label);
        }
    }
    
    private void ClosePreview()
    {
        if (IsInstanceValid(_popup))
        {
            _popup!.Hide();
            _popup.QueueFree();
        }
        _popup = null;
    }
}