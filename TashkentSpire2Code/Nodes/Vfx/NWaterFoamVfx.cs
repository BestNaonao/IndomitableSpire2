using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>
/// Bounded, world-positioned white foam for flying torpedoes only. A single
/// canvas item draws a fixed pool; no movement-wave assets or gameplay randomness.
/// </summary>
public partial class NWaterFoamVfx : Node2D
{
    public const string FlightAtlasPath = "res://TashkentSpire2/images/vfx/water/azurlane_torpedo_foam_4x4.png";
    public const string ShaderPath = "res://TashkentSpire2/shaders/white_water_foam.gdshader";
    public static IEnumerable<string> AssetPaths => [FlightAtlasPath, ShaderPath];
    private const int Capacity = 24;
    private const int MaxSamplesPerFrame = 12;

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
    private Texture2D _atlas = null!;
    private Vector2 _previousPosition;
    private float _distanceCarry;
    private int _nextParticle;
    private int _emitted;
    private bool _active;

    public static NWaterFoamVfx? StartFlight(Node2D parent, Node2D projectile, Creature owner)
    {
        if (!CanPlay(owner))
            return null;
        NWaterFoamVfx? vfx = parent.GetChildren().OfType<NWaterFoamVfx>()
            .FirstOrDefault(node => !node.IsQueuedForDeletion());
        if (vfx == null)
        {
            vfx = new NWaterFoamVfx { Name = "TashkentTorpedoFoam" };
            parent.AddChildSafely(vfx);
            parent.MoveChildSafely(vfx, 0);
        }
        vfx._owner = owner;
        vfx._projectile = projectile;
        vfx.Restart(vfx.GetProjectileTail());
        return vfx;
    }

    public override void _EnterTree() => CombatManager.Instance.CombatEnded += OnCombatEnded;

    public override void _Ready()
    {
        _atlas = PreloadManager.Cache.GetTexture2D(FlightAtlasPath);
        Material = new ShaderMaterial { Shader = PreloadManager.Cache.GetAsset<Shader>(ShaderPath) };
        TextureFilter = TextureFilterEnum.Linear;
        SetProcess(false);
    }

    private void Restart(Vector2 position)
    {
        Array.Clear(_foam);
        _nextParticle = 0;
        _emitted = 0;
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
            || _projectile == null || !GodotObject.IsInstanceValid(_projectile)
            || _projectile.IsQueuedForDeletion() || !_projectile.IsInsideTree())
        {
            StopImmediately();
            return;
        }

        float dt = Math.Max(0f, (float)delta);
        for (int i = 0; i < Capacity; i++)
        {
            ref Foam foam = ref _foam[i];
            if (!foam.Active)
                continue;
            foam.Age += dt;
            foam.Position += foam.Velocity * dt;
            foam.Active = foam.Age < foam.Lifetime;
        }

        Vector2 position = GetProjectileTail();
        Vector2 displacement = position - _previousPosition;
        float distance = displacement.Length();
        if (distance > 0.01f)
        {
            Vector2 direction = displacement / distance;
            const float spacing = 18f;
            _distanceCarry += distance;
            int count = Math.Min(MaxSamplesPerFrame, (int)(_distanceCarry / spacing));
            if (count > 0)
            {
                _distanceCarry %= spacing;
                for (int i = 0; i < count; i++)
                {
                    float fraction = (i + 1f) / count;
                    Vector2 sample = _previousPosition.Lerp(position, fraction);
                    Emit(sample, direction, (1f - fraction) * Math.Min(dt, 0.08f));
                }
            }
        }
        _previousPosition = position;
        QueueRedraw();
    }

    private void Emit(Vector2 position, Vector2 direction, float age)
    {
        float variation = Mathf.Sin(++_emitted * 2.399963f);
        Vector2 side = new(-direction.Y, direction.X);
        _foam[_nextParticle] = new Foam
        {
            Active = true,
            Position = position + side * variation * 5f,
            Velocity = -direction * 28f + side * variation * 15f,
            Age = age,
            Lifetime = 0.22f,
            Size = 48f + variation * 10f,
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
                new Color(1f, 1f, 1f, (1f - progress) * 0.85f));
        }

        DrawSetTransform(Vector2.Zero);
    }

    private Vector2 GetProjectileTail()
    {
        return _projectile!.GlobalPosition - Vector2.FromAngle(_projectile.GlobalRotation)
            * (58f * _projectile.GlobalTransform.X.Length());
    }

    public void StopImmediately()
    {
        _active = false;
        Visible = false;
        SetProcess(false);
        Array.Clear(_foam);
        QueueRedraw();
        // Flights retain one empty renderer for TorpedoGod's next target; the
        // torpedo parent owns it.
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
    }
}
