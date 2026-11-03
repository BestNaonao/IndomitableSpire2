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

/// <summary>One bounded, non-blocking flash/flame/smoke controller per attacking character.</summary>
public partial class NAmmunitionMuzzleVfx : Node2D
{
    public const string FlashPath = "res://TashkentSpire2/images/vfx/muzzle/azurlane_muzzle_flash.png";
    public const string FlamePath = "res://TashkentSpire2/images/vfx/muzzle/azurlane_muzzle_flame.png";
    public const string SmokePath = "res://TashkentSpire2/images/vfx/muzzle/azurlane_muzzle_smoke.png";
    public static IEnumerable<string> AssetPaths => [FlashPath, FlamePath, SmokePath];
    private const float ShotSpacing = 0.075f;
    private const float EffectScale = 2f;
    private const int MaxPendingShots = 12;
    private const int SmokeCount = 8;

    private struct Smoke
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Age;
        public float Rotation;
        public bool Active;
    }

    private NCreature? _owner;
    private Node2D _flashRoot = null!;
    private Sprite2D _flash = null!;
    private Sprite2D _flame = null!;
    private readonly Sprite2D[] _smokeSprites = new Sprite2D[SmokeCount];
    private readonly Smoke[] _smoke = new Smoke[SmokeCount];
    private Vector2 _muzzleWorld;
    private float _shotAge = 1f;
    private int _pending;
    private int _shotIndex;
    private int _nextSmoke;

    public static void Play(Creature attacker)
    {
        if (!CanPlay(attacker))
            return;
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(attacker);
        if (node == null)
            return;
        var vfx = node.GetChildren().OfType<NAmmunitionMuzzleVfx>()
            .FirstOrDefault(effect => !effect.IsQueuedForDeletion());
        if (vfx == null)
        {
            vfx = new NAmmunitionMuzzleVfx { Name = "TashkentAmmunitionMuzzleVfx" };
            node.AddChildSafely(vfx);
            Control? health = node.GetNodeOrNull<Control>("HealthBar");
            if (health != null && health.GetParent() == node)
                node.MoveChildSafely(vfx, health.GetIndex());
        }
        // The first shot starts now. Very fast hits get a small visual-only queue,
        // so six same-frame hits do not collapse into one continuous bright blob.
        if (vfx._shotAge >= ShotSpacing && vfx._pending == 0)
            vfx.FireShot();
        else
            vfx._pending = Math.Min(MaxPendingShots, vfx._pending + 1);
    }

    public override void _EnterTree() => CombatManager.Instance.CombatEnded += OnCombatEnded;

    public override void _Ready()
    {
        _owner = GetParent() as NCreature;
        Texture2D smokeTexture = PreloadManager.Cache.GetTexture2D(SmokePath);
        for (int i = 0; i < SmokeCount; i++)
        {
            _smokeSprites[i] = new Sprite2D { Name = $"Smoke{i}", Texture = smokeTexture, Visible = false };
            AddChild(_smokeSprites[i]);
        }
        _flashRoot = new Node2D { Name = "Muzzle", Visible = false };
        AddChild(_flashRoot);
        CanvasItemMaterial additive = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _flame = new Sprite2D
        {
            Name = "Flame", Texture = PreloadManager.Cache.GetTexture2D(FlamePath),
            Material = additive, FlipH = true, Position = new Vector2(52f, 0f)
        };
        _flashRoot.AddChild(_flame);
        _flash = new Sprite2D
        {
            Name = "Flash", Texture = PreloadManager.Cache.GetTexture2D(FlashPath),
            Material = additive, Position = new Vector2(20f, 0f)
        };
        _flashRoot.AddChild(_flash);
    }

    private void FireShot()
    {
        if (_owner == null)
            return;
        float facing = 1f;
        Node2D body = _owner.Body;
        if (GodotObject.IsInstanceValid(body) && !Mathf.IsZeroApprox(body.Scale.X))
            facing = Mathf.Sign(body.Scale.X);
        Control bounds = _owner.Visuals.Bounds;
        float width = (bounds.GetGlobalTransform().X * bounds.Size.X).Length();
        _muzzleWorld = _owner.VfxSpawnPosition
            + new Vector2(facing * Mathf.Clamp(width * 0.45f, 90f, 155f), 18f);
        _flashRoot.GlobalPosition = _muzzleWorld;
        _flashRoot.Scale = new Vector2(facing, 1f) * EffectScale;
        _flashRoot.Visible = true;
        _shotAge = 0f;
        _shotIndex++;
        UpdateFlash();
        for (int i = 0; i < 2; i++)
        {
            float variation = Mathf.Sin((_shotIndex * 2 + i) * 2.399963f);
            _smoke[_nextSmoke] = new Smoke
            {
                Active = true, Position = _muzzleWorld + new Vector2(facing * (16f + i * 14f), 0f) * EffectScale,
                Velocity = new Vector2(facing * (24f + i * 10f), -22f - i * 7f) * EffectScale,
                Age = -i * 0.035f, Rotation = variation * 0.65f
            };
            _nextSmoke = (_nextSmoke + 1) % SmokeCount;
        }
    }

    public override void _Process(double delta)
    {
        if (_owner == null || !GodotObject.IsInstanceValid(_owner) || !CanPlay(_owner.Entity))
        {
            StopImmediately();
            return;
        }
        float dt = Math.Max(0f, (float)delta);
        _shotAge += dt;
        for (int i = 0; i < SmokeCount; i++)
        {
            ref Smoke smoke = ref _smoke[i];
            smoke.Age += dt;
            smoke.Active &= smoke.Age < 0.56f;
            Sprite2D sprite = _smokeSprites[i];
            sprite.Visible = smoke.Active && smoke.Age >= 0f;
            if (!sprite.Visible)
                continue;
            smoke.Position += smoke.Velocity * dt;
            float progress = smoke.Age / 0.56f;
            sprite.GlobalPosition = smoke.Position;
            sprite.Rotation = smoke.Rotation + progress * 0.4f;
            sprite.Scale = Vector2.One * (EffectScale * (28f + progress * 45f) / sprite.Texture.GetWidth());
            sprite.Modulate = new Color(0.7f, 0.72f, 0.76f,
                Mathf.Clamp(smoke.Age / 0.05f, 0f, 1f) * (1f - progress) * 0.38f);
        }
        if (_pending > 0 && _shotAge >= ShotSpacing)
        {
            _pending--;
            FireShot();
        }
        UpdateFlash();
        if (_pending == 0 && _shotAge > 0.65f)
            StopImmediately();
    }

    private void UpdateFlash()
    {
        _flashRoot.Visible = _shotAge < 0.12f;
        float flashAlpha = Mathf.Clamp(1f - _shotAge / 0.06f, 0f, 1f);
        float flameAlpha = Mathf.Clamp(1f - _shotAge / 0.12f, 0f, 1f);
        _flash.Scale = new Vector2(95f, 65f) / _flash.Texture.GetSize();
        _flash.Modulate = new Color(1f, 0.9f, 0.65f, flashAlpha);
        _flame.Scale = new Vector2(125f + _shotAge * 100f, 50f) / _flame.Texture.GetSize();
        _flame.Modulate = new Color(1f, 0.9f, 0.8f, flameAlpha * 0.85f);
    }

    private static bool CanPlay(Creature creature) => !TestMode.IsOn && creature.IsAlive
        && CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding;

    private void OnCombatEnded(CombatRoom _) => StopImmediately();

    private void StopImmediately()
    {
        _pending = 0;
        Visible = false;
        SetProcess(false);
        this.QueueFreeSafely();
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        _owner = null;
    }
}
