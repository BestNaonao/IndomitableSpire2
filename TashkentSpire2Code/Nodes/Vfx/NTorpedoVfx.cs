using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>
/// A lightweight, model-bound combat visual for one TorpedoPower instance.
/// Placement and motion never consume gameplay RNG, so this cosmetic node cannot
/// disturb deterministic multiplayer state.
/// </summary>
public partial class NTorpedoVfx : Node2D
{
    public const string ScenePath = "res://TashkentSpire2/scenes/vfx/torpedo_vfx.tscn";

    private const float SpawnDuration = 0.22f;
    private const float FlightDuration = 0.3f;
    private const float DisplayCanvasWidth = 96f;

    private Node2D _torpedo = null!;
    private Node2D _facing = null!;
    private Sprite2D _sprite = null!;
    private NCreature? _ownerNode;

    private Tween? _spawnTween;
    private Tween? _flightTween;
    private Tween? _dismissTween;
    private TaskCompletionSource<bool>? _launchCompletion;

    private float _phase;
    private double _elapsed;
    private bool _isHovering;
    private bool _isLaunching;
    private bool _isFinishing;
    private float _lastFacingSign = 1f;

    public static IEnumerable<string> AssetPaths => [ScenePath];

    public TorpedoPower Power { get; private set; } = null!;
    public int DisplaySlot { get; private set; }
    public bool CanLaunch => !_isFinishing;

    // A flying final torpedo still occupies one of the twelve visible slots. The
    // slot becomes reusable only after impact, when the sprite starts disappearing.
    public bool CountsTowardDisplayLimit => !_isFinishing || _isLaunching;

    public override void _EnterTree()
    {
        base._EnterTree();
        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    public override void _Ready()
    {
        _torpedo = GetNode<Node2D>("Torpedo");
        _facing = GetNode<Node2D>("Torpedo/Facing");
        _sprite = GetNode<Sprite2D>("Torpedo/Facing/Sprite");
        _ownerNode = GetParent() as NCreature;

        // The old packed icon occupied a 64 px canvas at scale 2.5. A 96 px
        // canvas is exactly 60% of that on-screen footprint, independent of the
        // new high-resolution texture's source dimensions.
        if (_sprite.Texture != null && _sprite.Texture.GetWidth() > 0)
        {
            float scale = DisplayCanvasWidth / _sprite.Texture.GetWidth();
            _sprite.Scale = Vector2.One * scale;
        }
    }

    public override void _Process(double delta)
    {
        if (!_isHovering)
        {
            return;
        }

        _elapsed += delta;
        float time = (float)_elapsed;
        float facingSign = GetFacingSign();

        // Small incommensurate waves keep the rack alive without making twelve
        // sprites overlap or requiring any particle emitters.
        float driftX = Mathf.Sin(time * 0.91f + _phase) * 3.5f
                     + Mathf.Sin(time * 2.17f + _phase * 0.37f) * 1.5f;
        float driftY = Mathf.Cos(time * 1.13f + _phase * 0.71f) * 2.5f
                     + Mathf.Sin(time * 1.79f + _phase) * 1.25f;

        Vector2 targetPosition = new(driftX, driftY);
        _torpedo.Position = _torpedo.Position.Lerp(targetPosition, Mathf.Clamp((float)delta * 5.5f, 0f, 1f));
        _torpedo.Rotation = Mathf.DegToRad(Mathf.Sin(time * 0.83f + _phase) * 1.5f);
        _facing.Scale = new Vector2(facingSign, 1f);

        float pulse = 0.5f + Mathf.Sin(time * 2.4f + _phase) * 0.5f;
        _sprite.Modulate = new Color(1f, 0.97f + pulse * 0.03f, 0.93f + pulse * 0.07f, 1f);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        _spawnTween?.Kill();
        _flightTween?.Kill();
        _dismissTween?.Kill();
        CompleteLaunch();
    }

    public static NTorpedoVfx? Create(TorpedoPower power)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        NTorpedoVfx node = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NTorpedoVfx>(PackedScene.GenEditState.Disabled);
        node.Power = power;
        return node;
    }

    /// <summary>
    /// Starts at the character center, then settles into its assigned slot below
    /// the health bar. The root remains parented to NCreature so it follows it.
    /// </summary>
    public void BeginSpawn(Vector2 ownerCenter, Vector2 restPosition, int displaySlot)
    {
        GlobalPosition = restPosition;
        DisplaySlot = displaySlot;

        uint ownerId = Power.Owner.CombatId ?? 0u;
        uint seed = unchecked(ownerId * 0x9E3779B9u + (uint)(displaySlot + 1) * 0x85EBCA6Bu);
        _phase = Mathf.Tau * StableUnit(seed ^ 0x27D4EB2Fu);

        _isHovering = false;
        _isLaunching = false;
        _isFinishing = false;
        _torpedo.GlobalPosition = ownerCenter;
        _torpedo.GlobalRotation = 0f;
        _torpedo.Scale = Vector2.Zero;
        _torpedo.Modulate = Colors.Transparent;
        _facing.Scale = new Vector2(GetFacingSign(), 1f);

        _spawnTween = CreateTween();
        _spawnTween.TweenProperty(_torpedo, "position", Vector2.Zero, SpawnDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        _spawnTween.Parallel().TweenProperty(_torpedo, "scale", Vector2.One, 0.18f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        _spawnTween.Parallel().TweenProperty(_torpedo, "modulate", Colors.White, 0.12f)
            .SetEase(Tween.EaseType.Out);
        _spawnTween.Chain().TweenCallback(Callable.From(() => _isHovering = true));
    }

    /// <summary>
    /// Accelerates to the synchronized enemy hit point in about 0.3 seconds.
    /// Non-final launches stay at the impact point for TorpedoGodPower's ordered
    /// traversal; the returned task completes exactly when the target is reached.
    /// </summary>
    public Task LaunchAsync(Vector2 targetPosition, bool isFinalTarget)
    {
        if (_isFinishing || !GodotObject.IsInstanceValid(_torpedo))
        {
            return Task.CompletedTask;
        }

        _spawnTween?.Kill();
        _flightTween?.Kill();
        CompleteLaunch();

        _isHovering = false;
        _isLaunching = true;
        _isFinishing = isFinalTarget;

        if (_torpedo.Scale.LengthSquared() < 0.1f)
        {
            _torpedo.Scale = Vector2.One * 0.75f;
        }
        _torpedo.Modulate = Colors.White;
        _facing.Scale = Vector2.One;

        Vector2 startPosition = _torpedo.GlobalPosition;
        Vector2 direction = targetPosition - startPosition;
        if (direction.LengthSquared() < 0.01f)
        {
            direction = Vector2.Right;
        }
        _torpedo.GlobalRotation = direction.Angle();

        _launchCompletion = new TaskCompletionSource<bool>();
        Task completionTask = _launchCompletion.Task;

        _flightTween = CreateTween();
        _flightTween.TweenProperty(_torpedo, "global_position", targetPosition, FlightDuration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Expo);
        _flightTween.Parallel().TweenProperty(_torpedo, "scale", Vector2.One * 1.08f, FlightDuration)
            .SetEase(Tween.EaseType.In);
        _flightTween.Chain().TweenCallback(Callable.From(OnImpact));

        if (isFinalTarget)
        {
            _flightTween.TweenCallback(Callable.From(this.QueueFreeSafely));
        }
        else
        {
            _flightTween.TweenProperty(_torpedo, "scale", Vector2.One, 0.035f)
                .SetEase(Tween.EaseType.Out);
        }

        return completionTask;
    }

    public void Dismiss()
    {
        if (_isFinishing || !GodotObject.IsInstanceValid(_torpedo))
        {
            return;
        }

        _isFinishing = true;
        _isHovering = false;
        _isLaunching = false;
        _spawnTween?.Kill();
        _flightTween?.Kill();
        CompleteLaunch();

        _dismissTween = CreateTween();
        _dismissTween.TweenProperty(_torpedo, "scale", Vector2.Zero, 0.12f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
        _dismissTween.Parallel().TweenProperty(_torpedo, "modulate:a", 0f, 0.12f);
        _dismissTween.Chain().TweenCallback(Callable.From(this.QueueFreeSafely));
    }

    private void OnImpact()
    {
        _isLaunching = false;
        if (_isFinishing)
        {
            // The vanilla fire/smoke burst covers the impact. Hiding here makes
            // the vacated rack slot reusable without ever drawing a 13th sprite.
            _torpedo.Visible = false;
        }
        CompleteLaunch();
    }

    private void OnCombatEnded(CombatRoom _)
    {
        // Powers are not guaranteed to run AfterRemoved when the room tears down.
        // Stop immediately so no persistent creature child can leak into rewards.
        _isFinishing = true;
        _isHovering = false;
        _isLaunching = false;
        SetProcess(false);
        _spawnTween?.Kill();
        _flightTween?.Kill();
        _dismissTween?.Kill();
        if (_torpedo != null && GodotObject.IsInstanceValid(_torpedo))
        {
            _torpedo.Visible = false;
        }
        CompleteLaunch();
        this.QueueFreeSafely();
    }

    private void CompleteLaunch()
    {
        _launchCompletion?.TrySetResult(true);
        _launchCompletion = null;
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

    private static float StableUnit(uint value)
    {
        value ^= value >> 16;
        value *= 0x7FEB352Du;
        value ^= value >> 15;
        value *= 0x846CA68Bu;
        value ^= value >> 16;
        return (value & 0x00FFFFFFu) / 16777215f;
    }
}
