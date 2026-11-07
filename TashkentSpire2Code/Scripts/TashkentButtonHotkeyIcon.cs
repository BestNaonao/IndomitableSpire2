using Godot;

namespace TashkentSpire2.TashkentSpire2Code.Scripts;

/// <summary>Use the running game's glyph implementation, without changing any hotkey bindings.</summary>
internal static class TashkentButtonHotkeyIcon
{
    private const string NativeScene = "res://scenes/ui/hotkey_icon.tscn";

    public static void Prepare(Control button, NodePath parentPath, Vector2 position, float layoutScale = 1f)
    {
        TextureRect? legacy = button.GetNodeOrNull<TextureRect>("%ControllerIcon");
        // Older Stable builds still manage %ControllerIcon directly. Newer
        // builds manage %HotkeyIcon instead; don't leave the old A placeholder
        // visible beside it. Both paths retain the base button's input rules.
        if (!ResourceLoader.Exists(NativeScene) || button.GetNodeOrNull<Control>("%HotkeyIcon") != null)
            return;

        PackedScene? scene = ResourceLoader.Load<PackedScene>(NativeScene);
        if (scene == null) return;
        Control icon = scene.Instantiate<Control>();
        icon.Name = "HotkeyIcon";
        icon.Visible = false;
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        icon.Position = position * layoutScale + icon.PivotOffset * (layoutScale - 1f);
        icon.Size = new Vector2(48f, 48f);
        icon.Scale = Vector2.One * (0.85f * layoutScale);
        button.GetNode<Control>(parentPath).AddChild(icon);
        icon.Owner = button;
        icon.UniqueNameInOwner = true;
        if (legacy != null) legacy.Hide();
        // NButton.GetControllerIconNode + UpdateControllerButton now find the
        // native node and own its mouse / controller / keyboard-only visibility.
    }
}
