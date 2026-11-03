using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>
/// One shared-texture reticle per marked creature, independent of its applier or
/// Spine skin. No gameplay RNG, damage callbacks, network messages or awaited VFX.
/// </summary>
public partial class NMarkTargetVfx : Node2D
{
    public const string ReticlePath = "res://TashkentSpire2/images/vfx/mark/azurlane_mark_reticle.png";
    public static IEnumerable<string> AssetPaths =>
        [ReticlePath, NFireControlRadarVfx.RadarPath, NFireControlRadarVfx.SweepPath,
            NFireControlRadarVfx.SweepShaderPath];

    private readonly MarkTargetVfxTimeline _timeline = new();
    private NCreature? _ownerNode;
    private Sprite2D _reticle = null!;
    private float _visibility;
    private bool _dismissing;

    public static NMarkTargetVfx? Ensure(Creature target)
    {
        if (!CanPlay(target))
            return null;

        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (ownerNode == null)
            return null;

        NMarkTargetVfx? existing = Find(ownerNode);
        if (existing != null)
        {
            existing._dismissing = false;
            return existing;
        }

        NMarkTargetVfx vfx = new() { Name = "TashkentMarkTargetVfx" };
        ownerNode.AddChildSafely(vfx);
        // Official creature ordering is Visuals, HealthBar, Intents, etc. Insert
        // above the body but below its UI, including very small marked creatures.
        Control? healthBar = ownerNode.GetNodeOrNull<Control>("HealthBar");
        if (healthBar != null && healthBar.GetParent() == ownerNode)
            ownerNode.MoveChildSafely(vfx, healthBar.GetIndex());
        return vfx;
    }

    public static void PlayMarkAdded(Creature target)
    {
        Ensure(target)?._timeline.PlayMarkAdded();
    }

    public static void PlayFireControl(Creature target)
    {
        // May briefly lock an unmarked/Artifact-protected target as well. It will
        // disappear after confirmation unless a real MarkPower is present.
        Ensure(target)?._timeline.PlayFireControl();
    }

    public static void Dismiss(Creature target)
    {
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(target);
        NMarkTargetVfx? vfx = ownerNode == null ? null : Find(ownerNode);
        if (vfx == null)
            return;

        vfx._dismissing = true;
        vfx._timeline.Clear();
    }

    internal static bool CanPlay(Creature target) =>
        !TestMode.IsOn && target.IsAlive && CombatManager.Instance.IsInProgress
        && !CombatManager.Instance.IsEnding;

    private static NMarkTargetVfx? Find(NCreature node) => node.GetChildren()
        .OfType<NMarkTargetVfx>()
        .FirstOrDefault(vfx => GodotObject.IsInstanceValid(vfx) && !vfx.IsQueuedForDeletion());

    public override void _EnterTree()
    {
        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    public override void _Ready()
    {
        _ownerNode = GetParent() as NCreature;
        _reticle = new Sprite2D
        {
            Name = "Reticle",
            Texture = PreloadManager.Cache.GetTexture2D(ReticlePath),
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
        };
        AddChild(_reticle);
        Modulate = new Color(1f, 1f, 1f, 0f);
    }

    public override void _Process(double delta)
    {
        if (_ownerNode == null || !GodotObject.IsInstanceValid(_ownerNode)
            || !_ownerNode.Entity.IsAlive || !CombatManager.Instance.IsInProgress)
        {
            StopImmediately();
            return;
        }

        float step = Math.Max(0f, (float)delta);
        _timeline.Advance(step);
        bool hasMark = !_dismissing && (_ownerNode.Entity.GetPower<MarkPower>()?.Amount ?? 0) > 0;
        bool shouldShow = !_dismissing && (hasMark || _timeline.IsActive);
        _visibility = Mathf.MoveToward(_visibility, shouldShow ? 1f : 0f, step * 9f);
        Modulate = new Color(1f, 1f, 1f, _visibility);

        // A bounded cue at the existing hit center, not a full-creature overlay:
        // it never reaches out to the intent, health bar or card interaction UI.
        GlobalPosition = _ownerNode.VfxSpawnPosition;
        Control? bounds = _ownerNode.Visuals.Bounds;
        float diameter = GodotObject.IsInstanceValid(bounds)
            ? Mathf.Clamp(Mathf.Min(bounds!.Size.X, bounds.Size.Y) * 0.68f, 100f, 170f)
            : 132f;
        _reticle.Scale = Vector2.One * (diameter / _reticle.Texture.GetWidth()) * _timeline.Scale;
        float opacity = _dismissing ? 0.16f : _timeline.Opacity(hasMark);
        _reticle.Modulate = new Color(1f, 1f, 1f, opacity);

        if (!shouldShow && _visibility <= 0.001f)
            StopImmediately();
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        _ownerNode = null;
    }

    private void OnCombatEnded(CombatRoom _) => StopImmediately();

    private void StopImmediately()
    {
        Visible = false;
        SetProcess(false);
        this.QueueFreeSafely();
    }
}
