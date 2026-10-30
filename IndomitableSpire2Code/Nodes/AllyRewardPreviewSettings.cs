using Godot;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

/// <summary>奖励预览的手动调节区。均为 UI 参数，不影响实际奖励；修改后重新编译。</summary>
public static class AllyRewardPreviewSettings
{
    public const float IconSize = 60F;        // 图标正方形包围盒，参考原版充能球 60×60；保持原图比例。
    public const float IconGap = 12F;         // 相邻图标间隔，给右下角数字留出空间。
    public const float PlusWidth = 30F;       // 加号单独占用的宽度。
    public const float PlusIconGap = 6F;      // 加号与首个图标的间隔。
    public const int PlusFontSize = 36;       // 加号字号。
    public const float Opacity = 0.6F;        // 整行（加号、图标、数量）的统一透明度，0～1。
    public const float MarkerBlend = 0.5F;    // OrbPos→IntentPos 的插值：0 在 OrbPos，1 在 IntentPos。
    public static readonly Vector2 PositionOffset = new(0F, 0F); // 中点之外的局部偏移；Y 为负向上。
    public const bool MatchOrbScale = true;   // 跟随该角色 OrbManager 的缩放，适配远端角色体型。
    public const int ZIndex = 50;             // 高于角色本体，不拦截鼠标/手柄输入。
    
    // 数量排版参考原版 scenes/orbs/orb.tscn 的 PassiveAmount：Kreon Bold、24 号、12 像素描边。
    public const int AmountFontSize = 24;
    public const int AmountOutlineSize = 12;
    public static readonly Vector2 AmountOffset = new(-8F, -8F); // 数字中心相对图标右下角的偏移。
    public const string FontPath = "res://themes/kreon_bold_shared.tres";
}