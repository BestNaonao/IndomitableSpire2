using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>
/// Lightweight Warp Drive feedback: a persistent engine plume behind the owner
/// plus a short, turn-gated set of speed lines. All motion uses fixed phases and
/// a small reusable sprite pool, so it never consumes combat RNG.
/// </summary>
public partial class NWarpDriveVfx : Node2D
{
    public const string ScenePath = "res://TashkentSpire2/scenes/vfx/warp_drive_vfx.tscn";

    private static readonly Vector2[] ExhaustOffsets =
    [
        new(-70f, -101f),
        new(-103f, -109f),
        new(-139f, -94f),
        new(-174f, -104f)
    ];

    private static readonly float[] ExhaustPhases = [0f, 0.31f, 0.63f, 0.84f];
    private static readonly float[] SpeedLineY = [-220f, -182f, -145f, -106f, -67f, -29f];
    private static readonly float[] SpeedPhases = [0f, 0.44f, 0.16f, 0.72f, 0.31f, 0.87f];

    private Node2D _facing = null!;
    private Sprite2D _engineCore = null!;
    private Sprite2D[] _exhaustParticles = [];
    private Sprite2D[] _speedLines = [];
    private NCreature? _ownerNode;
    private float _elapsed;
    private float _visibility;
    private float _speedVisibility;
    private float _lastFacingSign = 1f;
    private bool _speedActive = true;
    private bool _dismissing;

    public static IEnumerable<string> AssetPaths => [ScenePath];

    public static NWarpDriveVfx? Create(Creature owner)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (ownerNode == null)
        {
            return null;
        }

        NWarpDriveVfx? existing = ownerNode.GetChildren()
            .OfType<NWarpDriveVfx>()
            .FirstOrDefault(vfx => GodotObject.IsInstanceValid(vfx) && !vfx.IsQueuedForDeletion());
        if (existing != null)
        {
            existing.Reactivate();
            return existing;
        }

        NWarpDriveVfx vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NWarpDriveVfx>(PackedScene.GenEditState.Disabled);
        ownerNode.AddChildSafely(vfx);
        ownerNode.MoveChildSafely(vfx, 0);
        vfx.Position = Vector2.Zero;
        return vfx;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    public override void _Ready()
    {
        _ownerNode = GetParent() as NCreature;
        _facing = GetNode<Node2D>("Facing");
        _engineCore = GetNode<Sprite2D>("Facing/EngineCore");
        _exhaustParticles = GetNode("Facing/ExhaustParticles")
            .GetChildren().OfType<Sprite2D>().ToArray();
        _speedLines = GetNode("Facing/SpeedLines")
            .GetChildren().OfType<Sprite2D>().ToArray();

        Modulate = new Color(1f, 1f, 1f, 0f);
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        float frameDelta = (float)delta;
        _elapsed += frameDelta;
        _visibility = Mathf.MoveToward(_visibility, _dismissing ? 0f : 1f, frameDelta * 3.2f);
        _speedVisibility = Mathf.MoveToward(
            _speedVisibility,
            _speedActive && !_dismissing ? 1f : 0f,
            frameDelta * (_speedActive ? 6f : 12f));

        Modulate = new Color(1f, 1f, 1f, _visibility);
        _facing.Scale = new Vector2(GetFacingSign(), 1f);

        AnimateEngine();
        AnimateSpeedLines();

        if (_dismissing && _visibility <= 0.001f)
        {
            SetProcess(false);
            this.QueueFreeSafely();
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
    }

    public void SetSpeedActive(bool isActive)
    {
        _speedActive = isActive;
        if (isActive && !_dismissing)
        {
            SetProcess(true);
        }
    }

    public void Dismiss()
    {
        if (_dismissing)
        {
            return;
        }

        _dismissing = true;
        _speedActive = false;
    }

    private void Reactivate()
    {
        _dismissing = false;
        Visible = true;
        SetProcess(true);
    }

    private void AnimateEngine()
    {
        float pulse = 0.5f + Mathf.Sin(_elapsed * 13.5f) * 0.5f;
        _engineCore.Position = new Vector2(-89f - pulse * 4f, -101f + Mathf.Sin(_elapsed * 9f) * 2f);
        _engineCore.Scale = new Vector2(0.094f + pulse * 0.008f, 0.071f + pulse * 0.006f);
        _engineCore.Modulate = new Color(0.83f, 0.96f, 1f, 0.56f + pulse * 0.22f);

        int count = Mathf.Min(_exhaustParticles.Length, ExhaustOffsets.Length);
        for (int i = 0; i < count; i++)
        {
            Sprite2D particle = _exhaustParticles[i];
            float progress = Mathf.PosMod(_elapsed * (1.62f + i * 0.08f) + ExhaustPhases[i], 1f);
            float fade = Mathf.Sin(progress * Mathf.Pi);
            float flutter = Mathf.Sin(_elapsed * (8.2f + i) + i * 1.7f);
            Vector2 origin = ExhaustOffsets[i];

            particle.Position = origin + new Vector2(-progress * (44f + i * 6f), flutter * (4f + i));
            particle.Scale = new Vector2(0.047f + progress * 0.034f, 0.036f + progress * 0.021f);
            particle.Rotation = Mathf.DegToRad(flutter * 3.2f);
            particle.Modulate = new Color(0.68f, 0.9f, 1f, fade * (0.3f + pulse * 0.2f));
        }
    }

    private void AnimateSpeedLines()
    {
        int count = Mathf.Min(_speedLines.Length, SpeedLineY.Length);
        for (int i = 0; i < count; i++)
        {
            Sprite2D line = _speedLines[i];
            float progress = Mathf.PosMod(_elapsed * (2.7f + i * 0.07f) + SpeedPhases[i], 1f);
            float envelope = Mathf.Sin(progress * Mathf.Pi);
            line.Position = new Vector2(116f - progress * 260f, SpeedLineY[i] + Mathf.Sin(_elapsed * 7f + i) * 3f);
            float length = 0.045f + (i % 3) * 0.009f;
            line.Scale = new Vector2(length * (0.82f + envelope * 0.3f), 0.036f + (i % 2) * 0.006f);
            line.Modulate = new Color(0.7f, 0.93f, 1f, _speedVisibility * envelope * 0.5f);
        }
    }

    private float GetFacingSign()
    {
        if (_ownerNode != null && GodotObject.IsInstanceValid(_ownerNode))
        {
            Node2D body = _ownerNode.Body;
            if (GodotObject.IsInstanceValid(body) && !Mathf.IsZeroApprox(body.Scale.X))
            {
                _lastFacingSign = Mathf.Sign(body.Scale.X);
            }
        }
        return _lastFacingSign;
    }

    private void OnCombatEnded(CombatRoom _)
    {
        _dismissing = true;
        _speedActive = false;
        SetProcess(false);
        Visible = false;
        this.QueueFreeSafely();
    }
}
