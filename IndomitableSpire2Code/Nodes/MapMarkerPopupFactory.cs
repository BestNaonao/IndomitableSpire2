using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Map;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>
/// 与 NHoverTipSet 一样按提示数据分派 View；文本复用 hover_tip.tscn 的容器/字体/九宫格，
/// 卡牌交给 NHoverTipCardContainer，敌人使用专用网格（类似 NMapPointHistoryHoverTip 的专用内容）。
/// 不调用 CreateAndShow：它固定挂在 NGame 全局 HoverTipsContainer，地图弹窗应留在地图之下的层级。
/// </summary>
public static class MapMarkerPopupFactory
{
    private const float TipWidth = 360f;
    private const float CellWidth = 144f;
    private const float CellHeight = 200f;
    private const float Padding = 14f;
    
    /// <summary>类型分派表</summary>
    private static readonly Dictionary<Type, Func<MapMarkerHoverContent, MapMarkerPopupLayout?>> Builders = new()
    {
        [typeof(MapMarkerTips)] = content => BuildTips((MapMarkerTips)content),
        [typeof(MapMarkerEnemies)] = content => BuildEnemies((MapMarkerEnemies)content)
    };
    
    public static void Register<T>(Func<T, MapMarkerPopupLayout?> builder) where T : MapMarkerHoverContent =>
        Builders[typeof(T)] = content => builder((T)content);
    
    /// <summary>唯一装配流程，按注册的内容类型分配构造方法。</summary>
    public static MapMarkerPopup? Create(Control owner, Control map, MapMarkerHoverContent? content)
    {
        if (content == null || NHoverTipSet.shouldBlockHoverTips ||
            !Builders.TryGetValue(content.GetType(), out var builder)) return null;
        var layout = builder(content);
        if (layout == null) return null;
        var root = layout.Root;
        map.AddChild(root); // 先入树，再创建依赖 _Ready 的卡牌/Spine Visual。
        try { layout.Populate(); }
        catch { root.Hide(); root.QueueFree(); throw; }
        IgnoreMouse(root);
        var popup = new MapMarkerPopup(root);
        popup.Reposition(owner);
        return popup;
    }
    
    // 递归忽略所有子节点的鼠标事件。
    private static void IgnoreMouse(Node node)
    {
        if (node is Control control) control.MouseFilter = Control.MouseFilterEnum.Ignore;
        foreach (var child in node.GetChildren()) IgnoreMouse(child);
    }
    
    private static MapMarkerPopupLayout? BuildTips(MapMarkerTips content)
    {
        if (content.Tips.Count == 0) return null;
        var root = new VBoxContainer { Name = "MapMarkerHoverTip", CustomMinimumSize = new Vector2(TipWidth, 0) };
        root.AddThemeConstantOverride("separation", 5);
        return new MapMarkerPopupLayout(root, () =>
        {
            foreach (var tip in IHoverTip.RemoveDupes(content.Tips))
            {
                if (tip is CardHoverTip card)
                {
                    var cards = new NHoverTipCardContainer(); root.AddChild(cards); cards.Add(card);
                    // NHoverTipCardContainer 的原版布局依赖全局 NGame；在本地父容器中报告实际子项边界。
                    var size = cards.GetChildren().OfType<Control>()
                        .Aggregate(Vector2.Zero, (s, c) => s.Max(c.Position + c.Size));
                    cards.CustomMinimumSize = size;
                    continue;
                }
                if (tip is not HoverTip text) continue;
                var panel = ResourceLoader.Load<PackedScene>("res://scenes/ui/hover_tip.tscn").Instantiate<Control>();
                root.AddChild(panel);
                var title = panel.GetNode<MegaLabel>("%Title");
                title.Visible = text.Title != null;
                title.ClipText = true;
                title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                if (text.Title != null) title.SetTextAutoSize(text.Title);
                var description = panel.GetNode<MegaRichTextLabel>("%Description");
                // 必须先设换行/最小宽度，再赋文本。FitContent 让 VBox/MarginContainer 根据换行自动增长高度。
                // 地图提示强制换行，忽略用于运行历史长表格的 ShouldOverrideTextOverflow。
                description.CustomMinimumSize = Vector2.Zero;
                description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                description.FitContent = true;
                description.ScrollActive = false;
                description.Text = text.Description;
                panel.GetNode<TextureRect>("%Icon").Texture = text.Icon;
                if (text.IsDebuff) panel.GetNode<CanvasItem>("%Bg").Material =
                    ResourceLoader.Load<Material>("res://materials/ui/hover_tip_debuff.tres");
                panel.CustomMinimumSize = new Vector2(TipWidth, 0);
                panel.ResetSize();
            }
            root.ResetSize();
        });
    }
    
    public static (int Columns, int Rows) GridSize(int count)
    {
        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));
        return (columns, Math.Max(1, (count + columns - 1) / columns));
    }
    
    private static MapMarkerPopupLayout? BuildEnemies(MapMarkerEnemies content)
    {
        var monsters = content.Monsters;
        if (monsters.Count == 0) return null;
        var (columns, rows) = GridSize(monsters.Count);
        var root = new NinePatchRect
        {
            Name = "EnemyPreview", Texture = ResourceLoader.Load<Texture2D>("res://images/ui/hover_tip.png"),
            SelfModulate = new Color(0.08f, 0.08f, 0.08f, 0.90f),
            PatchMarginLeft = 12, PatchMarginRight = 12, PatchMarginTop = 12, PatchMarginBottom = 12,
            Size = new Vector2(columns * CellWidth + Padding * 2, rows * CellHeight + Padding * 2)
        };
        return new MapMarkerPopupLayout(root, () =>
        {
            for (var i = 0; i < monsters.Count; i++)
            {
                var row = Math.DivRem(i, columns, out var col);
                var cell = new Control
                {
                    Position = new Vector2(Padding + col * CellWidth, Padding + row * CellHeight),
                    Size = new Vector2(CellWidth, CellHeight)
                };
                root.AddChild(cell);
                var label = new Label
                {
                    AutowrapMode = TextServer.AutowrapMode.WordSmart, ClipText = true,
                    Text = monsters[i].Title.GetFormattedText(),
                    Position = new Vector2(0, CellWidth),
                    Size = new Vector2(CellWidth, CellHeight - CellWidth),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                label.AddThemeFontSizeOverride("font_size", 16);
                cell.AddChild(label); label.Size = new Vector2(CellWidth, CellHeight - CellWidth);
                var portrait = new Control { Size = Vector2.One * CellWidth };
                cell.AddChild(portrait);
                try { EyeInTheSkyPortrait.Add(portrait, monsters[i], CellWidth); }
                catch (Exception exception)
                {
                    portrait.Hide(); portrait.QueueFree();
                    MainFile.Logger.Warn($"[EyeInTheSky] Portrait failed for {monsters[i].Id}: {exception}");
                }
            }
        });
    }
}

/// <summary>两阶段构造：先挂载 Root，再执行 Populate 进行填充，保证原版 Visual/卡牌的 _Ready 已执行。</summary>
public sealed record MapMarkerPopupLayout(Control Root, Action Populate);

/// <summary>统一弹窗句柄：负责跟随、屏幕边界和释放；具体内容由工厂决定。</summary>
public sealed class MapMarkerPopup(Control root) : IDisposable
{
    private const float Margin = 14f;
    
    public Control Root { get; } = root;
    
    public void Reposition(Control owner)
    {
        if (!GodotObject.IsInstanceValid(Root) || Root.IsQueuedForDeletion()) return;
        if (Root is Container) Root.Size = Root.GetCombinedMinimumSize();
        var viewport = owner.GetViewportRect().Size;
        var parent = (CanvasItem)Root.GetParent(); // 屏幕边界基准
        var parentScale = parent.GetGlobalTransformWithCanvas().Scale.Abs(); // 全局画布缩放的绝对值
        var naturalSize = Root.Size * parentScale; // 弹窗在屏幕上的"自然尺寸"
        var fit = Math.Min(1f, Math.Min((viewport.X - Margin * 2) / Math.Max(1, naturalSize.X),
            (viewport.Y - Margin * 2) / Math.Max(1, naturalSize.Y))); // 画布适配缩小比，只缩不放。
        Root.Scale = Vector2.One * Math.Max(.01f, fit); // 等比缩放，并设下限。
        var size = naturalSize * Root.Scale; // 最终屏幕尺寸
        var origin = owner.GetGlobalTransformWithCanvas().Origin; // 标记的左上角
        var position = origin + new Vector2(owner.Size.X * owner.GetGlobalTransformWithCanvas().Scale.X + Margin, 0);
        if (position.X + size.X > viewport.X - Margin) position.X = origin.X - size.X - Margin; // 若右溢出，则翻转左侧
        // 双轴钳进视口，保证不跑出屏幕。逆变换回父节点的局部坐标系，保持父节点跟随。
        position = position.Clamp(Vector2.One * Margin, (viewport - size - Vector2.One * Margin).Max(Vector2.One * Margin));
        Root.Position = parent.GetGlobalTransformWithCanvas().AffineInverse() * position;
    }
    
    public void Dispose()
    {
        if (!GodotObject.IsInstanceValid(Root)) return;
        Root.Hide(); Root.QueueFree();
    }
}