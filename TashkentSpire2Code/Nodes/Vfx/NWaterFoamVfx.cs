using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>
/// Bounded, world-positioned white foam. A single canvas item draws a fixed pool
/// from shared 4x4 atlases; no per-particle nodes, timers or gameplay randomness.
/// </summary>
public partial class NWaterFoamVfx : Node2D
{
    public const string FlightAtlasPath = "res://TashkentSpire2/images/vfx/water/azurlane_torpedo_foam_4x4.png";
    public const string MovementAtlasPath = "res://TashkentSpire2/images/vfx/water/azurlane_movement_foam_4x4.png";
    public const string WavePath = "res://TashkentSpire2/images/vfx/water/azurlane_short_wave.png";
    public const string ShaderPath = "res://TashkentSpire2/shaders/white_water_foam.gdshader";
    public static IEnumerable<string> AssetPaths => [FlightAtlasPath, MovementAtlasPath, WavePath, ShaderPath];
    private const int Capacity = 24;
    private const int MaxSamplesPerFrame = 12;
    private const float WaveLifetime = 0.48f;

    private struct Foam
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Age;
        public float Lifetime;
        public float Size;
        public float Rotation;
        public bool Active;
    }

    private readonly Foam[] _foam = new Foam[Capacity];
    private Creature? _owner;
    private Node2D? _projectile;
    private NCreature? _movingNode;
    private Texture2D _atlas = null!;
    private Texture2D? _wave;
    private Vector2 _previousPosition;
    private Vector2 _waveCenter;
    private Vector2 _moveDirection;
    private float _distanceCarry;
    private float _elapsed;
    private float _movementDuration;
    private int _nextParticle;
    private int _emitted;
    private bool _flight;
    private bool _active;

    public static NWaterFoamVfx? StartFlight(Node2D parent, Node2D projectile, Creature owner)
    {
        if (!CanPlay(owner))
            return null;
        NWaterFoamVfx? vfx = parent.GetChildren().OfType<NWaterFoamVfx>()
            .FirstOrDefault(node => node._flight && !node.IsQueuedForDeletion());
        if (vfx == null)
        {
            vfx = new NWaterFoamVfx { Name = "TashkentTorpedoFoam", _flight = true };
            parent.AddChildSafely(vfx);
            parent.MoveChildSafely(vfx, 0);
        }
        vfx._owner = owner;
        vfx._projectile = projectile;
        vfx.Restart(vfx.GetProjectileTail());
        return vfx;
    }

    public static void PlayMovement(NCreature node, float displacementX, float duration)
    {
        if (!CanPlay(node.Entity) || Mathf.IsZeroApprox(displacementX) || duration <= 0f)
            return;
        NWaterFoamVfx? vfx = node.GetChildren().OfType<NWaterFoamVfx>()
            .FirstOrDefault(effect => !effect._flight && !effect.IsQueuedForDeletion());
        if (vfx == null)
        {
            vfx = new NWaterFoamVfx { Name = "TashkentMovementFoam" };
            node.AddChildSafely(vfx);
            // Below health/intent UI, above the character body at its feet.
            Control? health = node.GetNodeOrNull<Control>("HealthBar");
            if (health != null && health.GetParent() == node)
                node.MoveChildSafely(vfx, health.GetIndex());
        }
        vfx._owner = node.Entity;
        vfx._movingNode = node;
        vfx._movementDuration = duration;
        vfx._moveDirection = new Vector2(Mathf.Sign(displacementX), 0f);
        vfx._waveCenter = vfx.GetFeetPosition();
        vfx.Restart(vfx._waveCenter);
        vfx.Emit(vfx._waveCenter - vfx._moveDirection * 20f, vfx._moveDirection, 0f);
    }

    public override void _EnterTree() => CombatManager.Instance.CombatEnded += OnCombatEnded;

    public override void _Ready()
    {
        _atlas = PreloadManager.Cache.GetTexture2D(_flight ? FlightAtlasPath : MovementAtlasPath);
        if (!_flight)
            _wave = PreloadManager.Cache.GetTexture2D(WavePath);
        Material = new ShaderMaterial { Shader = PreloadManager.Cache.GetAsset<Shader>(ShaderPath) };
        TextureFilter = TextureFilterEnum.Linear;
        SetProcess(false);
    }

    private void Restart(Vector2 position)
    {
        Array.Clear(_foam);
        _nextParticle = 0;
        _emitted = 0;
        _elapsed = 0f;
        _distanceCarry = 0f;
        _previousPosition = position;
        _active = true;
        Visible = true;
        SetProcess(true);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!_active)
            return;
        if (_owner == null || !CanPlay(_owner)
            || (_flight && (_projectile == null || !GodotObject.IsInstanceValid(_projectile)
                || _projectile.IsQueuedForDeletion() || !_projectile.IsInsideTree()))
            || (!_flight && (_movingNode == null || !GodotObject.IsInstanceValid(_movingNode)
                || _movingNode.IsQueuedForDeletion() || !_movingNode.IsInsideTree())))
        {
            StopImmediately();
            return;
        }

        float dt = Math.Max(0f, (float)delta);
        _elapsed += dt;
        for (int i = 0; i < Capacity; i++)
        {
            ref Foam foam = ref _foam[i];
            if (!foam.Active)
                continue;
            foam.Age += dt;
            foam.Position += foam.Velocity * dt;
            foam.Active = foam.Age < foam.Lifetime;
        }

        if (_flight || _elapsed <= _movementDuration + 0.001f)
        {
            Vector2 position = _flight ? GetProjectileTail() : GetFeetPosition();
            Vector2 displacement = position - _previousPosition;
            float distance = displacement.Length();
            if (distance > 0.01f)
            {
                Vector2 direction = displacement / distance;
                float spacing = _flight ? 18f : 13f;
                _distanceCarry += distance;
                int count = Math.Min(MaxSamplesPerFrame, (int)(_distanceCarry / spacing));
                if (count > 0)
                {
                    _distanceCarry %= spacing;
                    for (int i = 0; i < count; i++)
                    {
                        float fraction = (i + 1f) / count;
                        Vector2 sample = _previousPosition.Lerp(position, fraction);
                        if (!_flight)
                            sample -= _moveDirection * 22f;
                        Emit(sample, direction, (1f - fraction) * Math.Min(dt, 0.08f));
                    }
                }
            }
            _previousPosition = position;
            if (!_flight)
                _waveCenter = position;
        }

        QueueRedraw();
        if (!_flight && _elapsed >= Math.Max(WaveLifetime, _movementDuration + 0.4f))
            StopImmediately();
    }

    private void Emit(Vector2 position, Vector2 direction, float age)
    {
        float variation = Mathf.Sin(++_emitted * 2.399963f);
        Vector2 side = new(-direction.Y, direction.X);
        _foam[_nextParticle] = new Foam
        {
            Active = true,
            Position = position + side * variation * (_flight ? 5f : 9f),
            Velocity = -direction * (_flight ? 28f : 20f) + side * variation * 15f,
            Age = age,
            Lifetime = _flight ? 0.22f : 0.36f,
            Size = (_flight ? 48f : 75f) + variation * 10f,
            Rotation = direction.Angle() + variation * 0.3f
        };
        _nextParticle = (_nextParticle + 1) % Capacity;
    }

    public override void _Draw()
    {
        if (!_active || _atlas == null)
            return;
        Vector2 frameSize = _atlas.GetSize() / 4f;
        for (int i = 0; i < Capacity; i++)
        {
            ref Foam foam = ref _foam[i];
            if (!foam.Active)
                continue;
            float progress = Mathf.Clamp(foam.Age / foam.Lifetime, 0f, 0.9999f);
            int frame = Math.Min(15, (int)(progress * 16f));
            // Unity sequence UVs begin bottom-left; Godot texture regions begin top-left.
            Rect2 source = new(new Vector2(frame % 4, 3 - frame / 4) * frameSize, frameSize);
            Vector2 size = new Vector2(foam.Size * 1.25f, foam.Size * 0.65f) * (1f + progress * 0.4f);
            DrawSetTransform(ToLocal(foam.Position), foam.Rotation);
            DrawTextureRectRegion(_atlas, new Rect2(-size * 0.5f, size), source,
                new Color(1f, 1f, 1f, (1f - progress) * (_flight ? 0.85f : 0.75f)));
        }

        if (!_flight && _wave != null && _elapsed < WaveLifetime)
        {
            float progress = _elapsed / WaveLifetime;
            float alpha = Mathf.Clamp(_elapsed / 0.045f, 0f, 1f) * (1f - progress) * 0.85f;
            Vector2 size = new(165f + progress * 45f, 52f + progress * 12f);
            DrawSetTransform(ToLocal(_waveCenter + _moveDirection * 20f), 0f,
                new Vector2(_moveDirection.X, 1f));
            DrawTextureRect(_wave, new Rect2(-size * 0.5f + new Vector2(0f, -8f), size), false,
                new Color(1f, 1f, 1f, alpha));
        }
        DrawSetTransform(Vector2.Zero);
    }

    private Vector2 GetProjectileTail()
    {
        return _projectile!.GlobalPosition - Vector2.FromAngle(_projectile.GlobalRotation)
            * (58f * _projectile.GlobalTransform.X.Length());
    }

    private Vector2 GetFeetPosition()
    {
        Control bounds = _movingNode!.Visuals.Bounds;
        Transform2D transform = bounds.GetGlobalTransform();
        Vector2 a = transform * Vector2.Zero;
        Vector2 b = transform * new Vector2(bounds.Size.X, 0f);
        Vector2 c = transform * new Vector2(0f, bounds.Size.Y);
        Vector2 d = transform * bounds.Size;
        return new Vector2(((a + d) * 0.5f).X, Math.Max(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y)) - 5f);
    }

    public void StopImmediately()
    {
        _active = false;
        Visible = false;
        SetProcess(false);
        Array.Clear(_foam);
        QueueRedraw();
        // Flights retain one empty renderer for TorpedoGod's next target; the
        // torpedo parent owns it. Movement nodes expire with their short wake.
        if (!_flight)
            this.QueueFreeSafely();
    }

    private static bool CanPlay(Creature owner) => !TestMode.IsOn && owner.IsAlive
        && CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding;

    private void OnCombatEnded(CombatRoom _)
    {
        StopImmediately();
        this.QueueFreeSafely();
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        _active = false;
        _owner = null;
        _projectile = null;
        _movingNode = null;
    }
}
