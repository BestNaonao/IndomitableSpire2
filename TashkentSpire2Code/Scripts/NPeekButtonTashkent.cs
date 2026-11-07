using MegaCrit.Sts2.Core.Nodes.Combat;

namespace TashkentSpire2.TashkentSpire2Code.Scripts;

public partial class NPeekButtonTashkent : NPeekButton
{
    protected override void GetControllerIconNode()
    {
        TashkentButtonHotkeyIcon.Prepare(this, ".", new Godot.Vector2(-16f, 71f));
        base.GetControllerIconNode();
    }
}
