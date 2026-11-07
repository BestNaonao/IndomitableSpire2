using MegaCrit.Sts2.Core.Nodes.CommonUi;

using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;

namespace TashkentSpire2.TashkentSpire2Code.Scripts;

public partial class NConfirmButtonTashkent : NConfirmButton
{
    public override void _Ready()
    {
        base._Ready();
        RefreshCaption();
    }

    public void RefreshCaption()
    {
        var label = GetNodeOrNull<Label>("Image/ConfirmCaption/Text");
        if (label == null) return;
        label.Text = new LocString("gameplay_ui", "TASHKENTSPIRE2-SHELL_SELECTOR.confirm").GetFormattedText();
        var font = FontManager.GetSubstituteFont(LocManager.Instance?.Language ?? "eng", FontType.Bold)
            ?? ResourceLoader.Load<Font>("res://TashkentSpire2/fonts/kreon_bold.ttf");
        if (font != null) label.AddThemeFontOverride("font", font);
    }

    public override bool _HasPoint(Vector2 point)
    {
        // Extend only the hit test, not the root geometry/vanilla placement.
        var image = GetNodeOrNull<Control>("Image");
        var caption = image?.GetNodeOrNull<Control>("ConfirmCaption");
        return new Rect2(Vector2.Zero, Size).HasPoint(point) ||
            (image != null && caption != null &&
             new Rect2(image.Position + caption.Position, caption.Size).HasPoint(point));
    }

    protected override void GetControllerIconNode()
    {
        // The scene has doubled geometry, not a doubled root scale. Leave all
        // vanilla hover/press/unfocus scales (1.05 / 0.95 / 1) unchanged.
        TashkentButtonHotkeyIcon.Prepare(this, "Image", new Godot.Vector2(156f, 92f), 2f);
        base.GetControllerIconNode();
    }
}
