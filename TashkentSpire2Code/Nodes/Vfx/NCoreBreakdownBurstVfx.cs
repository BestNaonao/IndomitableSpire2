using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>Bounded, cosmetic icon shower. No gameplay RNG or per-particle nodes.</summary>
public partial class NCoreBreakdownBurstVfx : Node2D
{
    public const string TexturePath = "res://TashkentSpire2/images/vfx/core_breakdown/core_breakdown_particle.png";
    public static IEnumerable<string> AssetPaths => [TexturePath];
    private const int Capacity = 32;
    private const int BurstCount = 8;
    private const float Gravity = 1200f;
    private struct Particle
    {
        public bool Active;
        public Vector2 Origin, Velocity;
        public float Age, Lifetime, Size, Rotation, Spin, FloorY, LandingTime, BounceDuration, RestingRotation;
    }
    private readonly Particle[] _particles = new Particle[Capacity];
    private Texture2D _texture = null!;
    private int _next, _burstSerial;

    public static void Play(Creature target)
    {
        if (TestMode.IsOn || !target.IsAlive || !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding)
            return;
        var room = NCombatRoom.Instance;
        var owner = room?.GetCreatureNode(target);
        var container = room?.CombatVfxContainer;
        if (owner == null || !owner.IsInsideTree() || container == null || !container.IsInsideTree())
            return;
        var renderer = container.GetChildren().OfType<NCoreBreakdownBurstVfx>()
            .FirstOrDefault(n => !n.IsQueuedForDeletion());
        if (renderer == null)
        {
            renderer = new NCoreBreakdownBurstVfx { Name = "TashkentCoreBreakdownParticles" };
            container.AddChildSafely(renderer);
        }
        renderer.Emit(owner.VfxSpawnPosition, owner.GetBottomOfHitbox().Y);
    }

    public override void _EnterTree() => CombatManager.Instance.CombatEnded += OnCombatEnded;
    public override void _Ready()
    {
        _texture = PreloadManager.Cache.GetTexture2D(TexturePath);
        TextureFilter = TextureFilterEnum.Linear;
        SetProcess(false);
    }

    private void Emit(Vector2 origin, float floorY)
    {
        int serial = _burstSerial++;
        for (int i = 0; i < BurstCount; i++)
        {
            // A small upward shower, not a radial blast. No combat RNG is consumed.
            float variation = ((i * 17 + serial * 7) % 23) / 22f;
            float size = Mathf.Lerp(56f, 78f, variation);
            float ground = Math.Max(floorY - size * 0.15f, origin.Y + 16f);
            Vector2 velocity = new(Mathf.Lerp(-155f, 155f, i / (BurstCount - 1f)), -Mathf.Lerp(240f, 360f, variation));
            float landing = (-velocity.Y + Mathf.Sqrt(velocity.Y * velocity.Y + 2f * Gravity * (ground - origin.Y))) / Gravity;
            float bounce = Mathf.Lerp(0.16f, 0.22f, variation);
            _particles[_next] = new Particle
            {
                Active = true, Origin = origin,
                Velocity = velocity, FloorY = ground, LandingTime = landing, BounceDuration = bounce,
                Lifetime = landing + bounce + 0.5f, Size = size,
                Rotation = (variation - 0.5f) * 0.8f,
                RestingRotation = (variation - 0.5f) * 0.5f,
                Spin = (i % 2 == 0 ? 1f : -1f) * Mathf.Lerp(3f, 7f, variation)
            };
            _next = (_next + 1) % Capacity;
        }
        SetProcess(true);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding)
        {
            Stop();
            return;
        }
        bool active = false;
        for (int i = 0; i < Capacity; i++)
        {
            ref Particle particle = ref _particles[i];
            if (!particle.Active)
                continue;
            particle.Age += Math.Max(0f, (float)delta);
            if (particle.Age >= particle.Lifetime)
                particle = default;
            else
                active = true;
        }
        QueueRedraw();
        if (!active)
            SetProcess(false);
    }

    public override void _Draw()
    {
        Transform2D worldToLocal = GetGlobalTransform().AffineInverse();
        foreach (Particle particle in _particles)
        {
            if (!particle.Active)
                continue;
            float t = particle.Age;
            float progress = t / particle.Lifetime;
            Vector2 position = SamplePosition(particle);
            float resting = Mathf.Clamp((t - particle.LandingTime) / particle.BounceDuration, 0f, 1f);
            float rotation = Mathf.LerpAngle(particle.Rotation + particle.Spin * Math.Min(t, particle.LandingTime),
                particle.RestingRotation, resting);
            var world = new Transform2D(rotation, position);
            // Flatten during the turn like tossed coins, without disappearing edge-on.
            world.X *= Mathf.Lerp(0.2f + 0.8f * Mathf.Abs(Mathf.Cos(t * particle.Spin * 2f)), 1f, resting);
            world.Y *= Mathf.Lerp(1f, 0.65f, resting);
            DrawSetTransformMatrix(worldToLocal * world);
            float size = particle.Size * Mathf.Lerp(1f, 0.8f, progress);
            float alpha = Mathf.Clamp((particle.Lifetime - t) / 0.3f, 0f, 1f);
            DrawTextureRect(_texture, new Rect2(Vector2.One * -size * 0.5f, Vector2.One * size),
                false, new Color(1f, 1f, 1f, alpha));
        }
        DrawSetTransformMatrix(Transform2D.Identity);
    }

    // Analytic flight and one low bounce: large frame steps cannot tunnel below ground.
    private static Vector2 SamplePosition(Particle particle)
    {
        float t = particle.Age;
        if (t < particle.LandingTime)
            return particle.Origin + particle.Velocity * t + new Vector2(0f, Gravity * 0.5f * t * t);
        float afterLanding = t - particle.LandingTime;
        float bounceT = Mathf.Clamp(afterLanding / particle.BounceDuration, 0f, 1f);
        float x = particle.Origin.X + particle.Velocity.X * particle.LandingTime
            + particle.Velocity.X * 0.016f * (1f - Mathf.Exp(-afterLanding * 10f));
        float y = particle.FloorY - 32f * bounceT * (1f - bounceT);
        return new Vector2(x, y);
    }

    private void OnCombatEnded(CombatRoom _) => Stop();
    private void Stop()
    {
        Array.Clear(_particles);
        Visible = false;
        SetProcess(false);
        this.QueueFreeSafely();
    }
    public override void _ExitTree() => CombatManager.Instance.CombatEnded -= OnCombatEnded;
}
