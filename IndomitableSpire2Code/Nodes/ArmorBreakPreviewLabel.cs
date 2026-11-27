using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

internal partial class ArmorBreakPreviewLabel : Label
{
    /// <summary>静态工厂方法：尝试在指定敌人节点上创建预览标签</summary>
    internal static ArmorBreakPreviewLabel? Create(NCreature target)
    {
        var block = target.GetNodeOrNull<NCreatureStateDisplay>("%HealthBar")?
            .GetNodeOrNull<NHealthBar>("%HealthBar")?.GetNodeOrNull<Control>("%BlockContainer");
        if (block?.GetNodeOrNull<Label>("BlockLabel") is not { } number) return null; // 用于复用字体和字号
        var label = new ArmorBreakPreviewLabel
        {
            Name = "IndomitableArmorBreakPreview",
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            AnchorBottom = 1, // 底部锚点固定
            OffsetLeft = -180,
            OffsetRight = -4,
            GrowHorizontal = GrowDirection.Begin // 水平生长方向向左
        };
        label.AddThemeFontOverride("font", number.GetThemeFont("font"));
        label.AddThemeFontSizeOverride("font_size", number.GetThemeFontSize("font_size"));
        label.AddThemeConstantOverride("outline_size", 5);
        block.AddChild(label);
        return label;
    }
    
    internal void ShowLoss(int loss)
    {
        var text = new LocString("powers", "INDOMITABLESPIRE2-ARMOR_BREAK_POWER.blockLossPreview");
        text.Add("Amount", loss);
        Text = text.GetFormattedText();
        Show();
    }
}