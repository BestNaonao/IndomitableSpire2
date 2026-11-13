using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>Read-only encounter integration: owns only the ink overlay, never the original scenery or actors.</summary>
public partial class NPoseidonFormVfx : Control
{
    public const string TexturePath = "res://TashkentSpire2/images/backgrounds/poseidon/star_level_bg_598.png";
    public const string ShaderPath = "res://TashkentSpire2/shaders/poseidon_ink_reveal.gdshader";
    public static IEnumerable<string> AssetPaths => [TexturePath, ShaderPath];
    private const float FadeDuration = 1.8f / 1.3f;
    private NCombatRoom _room = null!;
    private Node2D? _surface;
    private TextureRect? _image;
    private ShaderMaterial? _ink;
    private bool _canDraw, _stopped;
    private float _opacity, _target, _pollTime;

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
            controller = new NPoseidonFormVfx { Name = "TashkentPoseidonBackground",
                _room = room, MouseFilter = MouseFilterEnum.Ignore };
            room.AddChildSafely(controller);
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
        if (_stopped) return;
        _target = CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding
            && _room.CreatureNodes.Any(n => n.Entity.Player != null && n.Entity.IsAlive
                && n.Entity.GetPowerAmount<PoseidonFormPower>() > 0) ? 1f : 0f;
        if (_target > 0f || _opacity > 0f)
        {
            EnsureOverlay();
            PlaceOverlay();
        }
    }

    private void EnsureOverlay()
    {
        if (GodotObject.IsInstanceValid(_surface) && !_surface!.IsQueuedForDeletion()
            && GodotObject.IsInstanceValid(_image) && !_image!.IsQueuedForDeletion())
            return;
        HideOverlay();
        if (GodotObject.IsInstanceValid(_surface)) _surface!.QueueFreeSafely();
        // A replaced background may have freed our child too. Recreate only our
        // own presentation; never resurrect, duplicate or replace its actors.
        _ink ??= new ShaderMaterial { Shader = PreloadManager.Cache.GetAsset<Shader>(ShaderPath) };
        _surface = new Node2D { Name = "TashkentPoseidonOverlay" };
        _image = new TextureRect { Name = "Seascape", MouseFilter = MouseFilterEnum.Ignore,
            Texture = PreloadManager.Cache.GetTexture2D(TexturePath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            Material = _ink, Visible = false };
        AddChild(_surface);
        _surface.AddChild(_image);
    }

    private void PlaceOverlay()
    {
        _canDraw = false;
        var background = _room.Background;
        if (!GodotObject.IsInstanceValid(_surface) || _surface!.IsQueuedForDeletion()
            || !GodotObject.IsInstanceValid(background) || !background!.IsInsideTree()
            || background.IsQueuedForDeletion()
            || background.GetCanvas() != _room.SceneContainer.GetCanvas())
            return;

        if (_surface.GetParent() == null)
            background.AddChildSafely(_surface);
        else if (_surface.GetParent() != background)
            _surface.Reparent(background, keepGlobalTransform: false);

        // Only Layer_00..NN are presumed to be scenery. Foreground, background-
        // owned bosses, and unknown mod branches stay AFTER us in tree order.
        // No hard-coded boss class, node path, texture or material is required.
        Node? boundary = null;
        foreach (Node child in background.GetChildren())
        {
            if (child == _surface || child.IsQueuedForDeletion()) continue;
            if (IsSceneryLayer(child) && !ContainsActor(child)) continue;
            boundary = child;
            break;
        }
        int index = boundary == null ? background.GetChildCount() - 1
            : boundary.GetIndex() - (_surface.GetIndex() < boundary.GetIndex() ? 1 : 0);
        if (_surface.GetIndex() != index)
            background.MoveChildSafely(_surface, index);

        int parentDepth = EffectiveZ(background);
        int maximumDepth = EffectiveZ(_room.SceneContainer);
        foreach (var actor in _room.CreatureNodes)
            if (GodotObject.IsInstanceValid(actor))
                maximumDepth = MinimumDepth(actor, maximumDepth);
        if (boundary != null)
        {
            bool afterOverlay = false;
            foreach (Node child in background.GetChildren())
            {
                if (child == boundary) afterOverlay = true;
                if (afterOverlay) maximumDepth = MinimumDepth(child, maximumDepth);
            }
        }
        // Lower ONLY our overlay if another mod lowers an actor. Higher scenery
        // can remain visible in that case: preserving actors wins over coverage.
        int localDepth = Math.Min(0, maximumDepth - parentDepth);
        if (localDepth < RenderingServer.CanvasItemZMin) return;
        _surface.ZIndex = localDepth;
        _canDraw = true;
    }

    private static bool IsSceneryLayer(Node node)
    {
        string name = node.Name.ToString();
        return node is Control && name.Length == 8
            && name.StartsWith("Layer_", StringComparison.Ordinal)
            && char.IsAsciiDigit(name[6]) && char.IsAsciiDigit(name[7]);
    }

    private static bool ContainsActor(Node node)
    {
        if (node is NCreature || node.GetClass() == "SpineSprite") return true;
        foreach (Node child in node.GetChildren())
            if (ContainsActor(child)) return true;
        return false;
    }

    private static int MinimumDepth(Node node, int minimum)
    {
        if (node is CanvasLayer || node is SubViewport) return minimum;
        if (node is CanvasItem item)
        {
            if (!item.IsVisibleInTree()) return minimum;
            minimum = Math.Min(minimum, EffectiveZ(item));
        }
        foreach (Node child in node.GetChildren())
            minimum = MinimumDepth(child, minimum);
        return minimum;
    }

    private static int EffectiveZ(CanvasItem item)
    {
        int depth = item.ZIndex;
        while (item.ZAsRelative && !item.TopLevel && item.GetParent() is CanvasItem parent)
        {
            depth += parent.ZIndex;
            item = parent;
        }
        return depth;
    }

    public override void _Process(double delta)
    {
        if (_stopped) return;
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
            // Restored powers, teammate changes, late actors and modded backgrounds.
            RefreshState();
        }
        _opacity = Mathf.MoveToward(_opacity, _target, dt / FadeDuration);
        if (!GodotObject.IsInstanceValid(_image) || !GodotObject.IsInstanceValid(_surface))
            return;
        if (_surface!.GetParent() != _room.Background) PlaceOverlay();
        _image!.Visible = _canDraw && _opacity > 0f;
        _ink!.SetShaderParameter("progress", _opacity);
        if (!_image.Visible || _surface.GetParent() is not CanvasItem parent) return;
        Transform2D parentTransform = parent.GetGlobalTransform();
        if (Mathf.IsZeroApprox(parentTransform.Determinant()))
        {
            _image.Visible = false;
            return;
        }
        // Keep normal tree sorting (no TopLevel), but counter-transform only our
        // own canvas so the image still fits the viewport through zoom/shake.
        _surface.Transform = parentTransform.AffineInverse() * _room.GetGlobalTransform();
        _image.Size = _room.Size;
    }

    private void OnCombatEnded(CombatRoom _) => Stop();

    private void Stop()
    {
        if (_stopped) return;
        _stopped = true;
        HideOverlay();
        SetProcess(false);
        this.QueueFreeSafely();
    }

    private void HideOverlay()
    {
        if (GodotObject.IsInstanceValid(_image)) _image!.Visible = false;
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        HideOverlay();
        if (GodotObject.IsInstanceValid(_surface))
            _surface!.QueueFreeSafely();
    }
}
