using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>One shared, nonblocking seascape per combat. No creature form sprites or particles.</summary>
public partial class NPoseidonFormVfx : Control
{
    public const string TexturePath = "res://TashkentSpire2/images/backgrounds/poseidon/star_level_bg_598.png";
    public const string ShaderPath = "res://TashkentSpire2/shaders/poseidon_ink_reveal.gdshader";
    public static IEnumerable<string> AssetPaths => [TexturePath, ShaderPath];
    // Nagato's current ink entrance: 1.8 / 1.3 seconds, without its later petals.
    private const float FadeDuration = 1.8f / 1.3f;
    private const int OceanDepth = -2; // Below creature bodies AND their -1 shadows.
    private NCombatRoom _room = null!;
    private TextureRect? _image;
    private ShaderMaterial? _ink;
    private Control? _original;
    private int _originalZ, _appliedZ;
    private float _opacity, _target, _pollTime;
    private bool _stopped, _hiddenOriginal, _changedZ;

    public static void Refresh(NCombatRoom? room = null)
    {
        room ??= NCombatRoom.Instance;
        if (TestMode.IsOn || room == null || !room.IsInsideTree()
            || room.Mode != CombatRoomMode.ActiveCombat || CombatManager.Instance.IsEnding)
            return;
        var controller = room.GetChildren().OfType<NPoseidonFormVfx>()
            .FirstOrDefault(n => !n.IsQueuedForDeletion());
        if (controller == null)
        {
            controller = new NPoseidonFormVfx { Name = "TashkentPoseidonBackground", _room = room,
                MouseFilter = MouseFilterEnum.Ignore, ZIndex = room.SceneContainer.ZIndex };
            room.AddChildSafely(controller);
            // Same depth as the official scene, but before ALL creatures and combat UI.
            room.MoveChildSafely(controller, room.SceneContainer.GetIndex());
        }
        controller.RefreshState();
    }

    public override void _EnterTree() => CombatManager.Instance.CombatEnded += OnCombatEnded;
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        RefreshState();
    }

    private void RefreshState()
    {
        if (_stopped)
            return;
        _target = CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding
            && _room.CreatureNodes.Any(n => n.Entity.Player != null && n.Entity.IsAlive
                && n.Entity.GetPowerAmount<PoseidonFormPower>() > 0) ? 1f : 0f;
        if (_target > 0f && _image == null)
        {
            _ink = new ShaderMaterial { Shader = PreloadManager.Cache.GetAsset<Shader>(ShaderPath) };
            _image = new TextureRect { Name = "Seascape", MouseFilter = MouseFilterEnum.Ignore,
                Texture = PreloadManager.Cache.GetTexture2D(TexturePath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                Material = _ink, TopLevel = true, ZAsRelative = false,
                ZIndex = _room.SceneContainer.ZIndex + OceanDepth, Visible = false };
            // Above the complete original background, below combat actors and
            // back VFX. Ink holes show the untouched scene, not a dark crossfade.
            _room.SceneContainer.AddChild(_image);
            _room.SceneContainer.MoveChildSafely(_image, 0);
        }
        CaptureBackground();
    }

    public override void _Process(double delta)
    {
        if (_stopped)
            return;
        if (CombatManager.Instance.IsEnding || _room.Mode != CombatRoomMode.ActiveCombat)
        {
            Stop();
            return;
        }
        float dt = Math.Max(0f, (float)delta);
        _pollTime += dt;
        if (_pollTime >= 0.1f)
        {
            _pollTime = 0f;
            // Covers restored powers, teammate changes, death/revival and late creature nodes.
            RefreshState();
        }
        _opacity = Mathf.MoveToward(_opacity, _target, dt / FadeDuration);
        if (_image != null)
        {
            _image.Visible = _opacity > 0f;
            _ink!.SetShaderParameter("progress", _opacity);
            // Stay viewport-fitted despite encounter zoom, offsets and shakes.
            Transform2D transform = _room.GetGlobalTransform();
            _image.GlobalPosition = transform.Origin;
            _image.Scale = transform.Scale;
            _image.Rotation = transform.Rotation;
            _image.Size = _room.Size;
            _image.ZIndex = _room.SceneContainer.ZIndex + OceanDepth;
        }
        ApplyOriginalVisibility();
    }

    private void CaptureBackground()
    {
        if (_original == _room.Background)
            return;
        RestoreOriginal();
        _original = _room.Background;
        _hiddenOriginal = false;
        _changedZ = false;
    }

    private static int HighestRelativeZ(Node parent)
    {
        int maximum = 0;
        foreach (Node child in parent.GetChildren())
        {
            int z = child is CanvasItem item && item.ZAsRelative ? item.ZIndex : 0;
            maximum = Math.Max(maximum, z + HighestRelativeZ(child));
        }
        return maximum;
    }

    private void ApplyOriginalVisibility()
    {
        if (!GodotObject.IsInstanceValid(_original))
            return;
        // A later depth edit belongs to its author, including edits between two
        // activations of this controller. Remember it before applying our depth.
        if (_changedZ && _original!.ZIndex != _appliedZ)
            _changedZ = false;
        if (_opacity > 0f && !_changedZ)
        {
            _originalZ = _original!.ZIndex;
            // Move the complete background subtree below the ink without
            // reparenting nodes or rewriting child materials/color/alpha.
            _appliedZ = Math.Min(_originalZ, OceanDepth - 1 - HighestRelativeZ(_original));
            _original!.ZIndex = _appliedZ;
            _changedZ = true;
        }
        else if (_opacity <= 0f)
            RestoreDepth();
        // Also suppress original lights/shaders after the fade. Alpha alone does
        // not disable a background Light2D or a shader that ignores vertex COLOR.
        if (_opacity >= 1f && _original!.Visible)
        {
            _original.Visible = false;
            _hiddenOriginal = true;
        }
        else if (_opacity < 1f && _hiddenOriginal)
        {
            _original.Visible = true;
            _hiddenOriginal = false;
        }
    }

    private void RestoreOriginal()
    {
        if (!GodotObject.IsInstanceValid(_original))
            return;
        if (_hiddenOriginal)
            _original!.Visible = true;
        _hiddenOriginal = false;
        RestoreDepth();
        _original = null;
    }

    private void RestoreDepth()
    {
        if (_changedZ && GodotObject.IsInstanceValid(_original) && _original!.ZIndex == _appliedZ)
            _original.ZIndex = _originalZ;
        _changedZ = false;
    }

    private void OnCombatEnded(CombatRoom _) => Stop();
    private void Stop()
    {
        if (_stopped)
            return;
        _stopped = true;
        Visible = false;
        if (GodotObject.IsInstanceValid(_image))
            _image!.Visible = false;
        RestoreOriginal();
        SetProcess(false);
        this.QueueFreeSafely();
    }
    public override void _ExitTree()
    {
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        RestoreOriginal();
        if (GodotObject.IsInstanceValid(_image))
        {
            _image!.Visible = false;
            _image.QueueFreeSafely();
        }
    }
}
