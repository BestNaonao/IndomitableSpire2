using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Backgrounds;
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
    private readonly List<BackgroundLayer> _backgroundLayers = [];
    private float _opacity, _target, _pollTime;
    private bool _stopped;

    private sealed class BackgroundLayer(CanvasItem node)
    {
        public CanvasItem Node { get; } = node;
        public int OriginalZ, AppliedZ;
        public bool Hidden, ChangedZ;
    }

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
            // Above scenery, below combat actors (including background-owned
            // bosses) and back VFX. Ink holes show the untouched scene.
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
        if (!GodotObject.IsInstanceValid(_original))
            return;
        // KaiserCrab is a live Spine actor, not scenery. Keep its original
        // parent, depth, visibility and bone/particle links completely intact.
        // Both supported game versions put it beside Layer_00..04/Foreground.
        if (_original!.GetNodeOrNull<NKaiserCrabBossBackground>("KaiserCrab") is { } crab)
        {
            foreach (Node child in _original.GetChildren())
                if (child is CanvasItem scenery && child != crab)
                    _backgroundLayers.Add(new BackgroundLayer(scenery));
        }
        else
            _backgroundLayers.Add(new BackgroundLayer(_original));
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
        foreach (BackgroundLayer layer in _backgroundLayers)
            ApplyLayerVisibility(layer);
    }

    private void ApplyLayerVisibility(BackgroundLayer layer)
    {
        if (!GodotObject.IsInstanceValid(layer.Node)) return;
        // A later depth edit belongs to its author, including edits between two
        // activations of this controller. Remember it before applying our depth.
        if (layer.ChangedZ && layer.Node.ZIndex != layer.AppliedZ)
            layer.ChangedZ = false;
        if (_opacity > 0f && !layer.ChangedZ)
        {
            layer.OriginalZ = layer.Node.ZIndex;
            // Account for nested scenery parents without lowering the boss's
            // shared parent. Ordinary encounters still lower their whole root.
            int parentZ = layer.Node.ZAsRelative ? EffectiveZ(layer.Node) - layer.Node.ZIndex : 0;
            layer.AppliedZ = Math.Min(layer.OriginalZ, _room.SceneContainer.ZIndex + OceanDepth
                - 1 - parentZ - HighestRelativeZ(layer.Node));
            layer.Node.ZIndex = layer.AppliedZ;
            layer.ChangedZ = true;
        }
        else if (_opacity <= 0f)
            RestoreDepth(layer);
        // Also suppress original lights/shaders after the fade. Alpha alone does
        // not disable a background Light2D or a shader that ignores vertex COLOR.
        if (_opacity >= 1f && layer.Node.Visible)
        {
            layer.Node.Visible = false;
            layer.Hidden = true;
        }
        else if (_opacity < 1f && layer.Hidden)
        {
            layer.Node.Visible = true;
            layer.Hidden = false;
        }
    }

    private static int EffectiveZ(CanvasItem item)
    {
        int depth = item.ZIndex;
        if (!item.ZAsRelative) return depth;
        for (Node? parent = item.GetParent(); parent != null; parent = parent.GetParent())
            if (parent is CanvasItem canvas)
            {
                depth += canvas.ZIndex;
                if (!canvas.ZAsRelative) break;
            }
        return depth;
    }

    private void RestoreOriginal()
    {
        foreach (BackgroundLayer layer in _backgroundLayers)
        {
            if (!GodotObject.IsInstanceValid(layer.Node)) continue;
            if (layer.Hidden) layer.Node.Visible = true;
            RestoreDepth(layer);
        }
        _backgroundLayers.Clear();
        _original = null;
    }

    private static void RestoreDepth(BackgroundLayer layer)
    {
        if (layer.ChangedZ && GodotObject.IsInstanceValid(layer.Node) && layer.Node.ZIndex == layer.AppliedZ)
            layer.Node.ZIndex = layer.OriginalZ;
        layer.ChangedZ = false;
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
