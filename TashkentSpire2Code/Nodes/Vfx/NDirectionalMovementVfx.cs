using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>Three short-lived arrows beside the moving body, independent of torpedo wakes.</summary>
public partial class NDirectionalMovementVfx : Node2D
{
    public const string TexturePath = "res://TashkentSpire2/images/vfx/movement/azurlane_direction_chevrons.png";
    public const string ShaderPath = "res://TashkentSpire2/shaders/directional_movement.gdshader";
    public static IEnumerable<string> AssetPaths => [TexturePath, ShaderPath];

    // Unrotated regions verified against zidan_zhenliejiantou.atlas.prefab.
    private static readonly Rect2[] ArrowRegions =
    [
        new(226f, 19f, 28f, 28f), new(256f, 19f, 27f, 28f), new(285f, 19f, 27f, 28f)
    ];

    private NCreature _owner = null!;
    private Texture2D _texture = null!;
    private ShaderMaterial _material = null!;
    private MegaSprite? _spine;
    private Callable _animationStarted;
    private float _direction;
    private float _duration;
    private float _age;
    private bool _stopped;

    public static void Play(NCreature owner, float displacementX, float duration, bool advancing)
    {
        if (TestMode.IsOn || !owner.Entity.IsAlive || owner.Entity.Player?.Character is not TashkentCharacter
            || !owner.IsInsideTree() || !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding
            || Mathf.IsZeroApprox(displacementX) || duration <= 0f)
            return;

        var effect = owner.GetChildren().OfType<NDirectionalMovementVfx>()
            .FirstOrDefault(node => !node.IsQueuedForDeletion());
        if (effect == null)
        {
            effect = new NDirectionalMovementVfx { Name = "TashkentMovementArrows", _owner = owner };
            owner.AddChildSafely(effect);
            Control? health = owner.GetNodeOrNull<Control>("HealthBar");
            if (health != null && health.GetParent() == owner)
                owner.MoveChildSafely(effect, health.GetIndex());
        }
        effect._direction = Mathf.Sign(displacementX);
        effect._duration = duration;
        effect._age = 0f;
        effect._material.SetShaderParameter("retreat", !advancing);
        effect.Visible = true;
        effect.SetProcess(true);
        effect.QueueRedraw();
    }

    public override void _EnterTree() => CombatManager.Instance.CombatEnded += OnCombatEnded;

    public override void _Ready()
    {
        _texture = PreloadManager.Cache.GetTexture2D(TexturePath);
        _material = new ShaderMaterial { Shader = PreloadManager.Cache.GetAsset<Shader>(ShaderPath) };
        Material = _material;
        TextureFilter = TextureFilterEnum.Linear;
        // DistancePower starts move/move_left before attaching this effect. Any
        // later animation owns the body, including attack/hit/death/victory/idle.
        if (_owner.Visuals.SpineBody is { } spine)
        {
            _animationStarted = Callable.From<GodotObject, GodotObject, GodotObject>(OnAnimationStarted);
            if (spine.ConnectAnimationStarted(_animationStarted) == Error.Ok)
                _spine = spine;
        }
    }

    public override void _Process(double delta)
    {
        if (_stopped)
            return;
        _age += Math.Max(0f, (float)delta);
        if (_age >= _duration || !_owner.Entity.IsAlive || !_owner.IsVisibleInTree()
            || !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding)
        {
            Stop();
            return;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_stopped || _texture == null || _duration <= 0f)
            return;
        Rect2 bounds = WorldBounds();
        // Positive displacement is in front for a right-facing advance, but
        // behind for a left-facing retreat. Use actual displacement, not a
        // hardcoded screen direction, so Surrounded and static skins also work.
        float edge = _direction > 0f ? bounds.End.X : bounds.Position.X;
        float y = bounds.End.Y - Mathf.Clamp(bounds.Size.Y * 0.28f, 50f, 95f);
        float phase = Mathf.Clamp(_age / _duration, 0f, 1f);
        float slide = phase * 28f;
        float fade = Mathf.Clamp((1f - phase) / 0.4f, 0f, 1f);
        Transform2D worldToLocal = GetGlobalTransform().AffineInverse();
        for (int i = 0; i < ArrowRegions.Length; i++)
        {
            float entrance = Mathf.Clamp((phase - i * 0.048f) / 0.14f, 0f, 1f);
            float pulse = 0.82f + 0.18f * Mathf.Sin(phase * 5.5f - i * 0.65f);
            Vector2 center = new(edge + _direction * (52f + i * 66f + slide), y);
            Transform2D world = new(new Vector2(_direction, 0f), Vector2.Down, center);
            DrawSetTransformMatrix(worldToLocal * world);
            DrawTextureRectRegion(_texture, new Rect2(-32f, -32f, 64f, 64f), ArrowRegions[i],
                new Color(1f, 1f, 1f, entrance * fade * pulse));
        }
        DrawSetTransformMatrix(Transform2D.Identity);
    }

    private Rect2 WorldBounds()
    {
        Control bounds = _owner.Visuals.Bounds;
        Transform2D transform = bounds.GetGlobalTransform();
        Vector2 a = transform * Vector2.Zero;
        Vector2 b = transform * new Vector2(bounds.Size.X, 0f);
        Vector2 c = transform * new Vector2(0f, bounds.Size.Y);
        Vector2 d = transform * bounds.Size;
        Vector2 minimum = a.Min(b).Min(c).Min(d);
        Vector2 maximum = a.Max(b).Max(c).Max(d);
        return new Rect2(minimum, maximum - minimum);
    }

    private void OnAnimationStarted(GodotObject _, GodotObject __, GodotObject ___) => Stop();
    private void OnCombatEnded(CombatRoom _) => Stop();
    private void Stop()
    {
        if (_stopped)
            return;
        _stopped = true;
        DisconnectWatcher();
        Visible = false;
        SetProcess(false);
        this.QueueFreeSafely();
    }
    private void DisconnectWatcher()
    {
        if (_spine != null && GodotObject.IsInstanceValid(_spine.BoundObject))
            _spine.DisconnectAnimationStarted(_animationStarted);
        _spine = null;
    }
    public override void _ExitTree()
    {
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        DisconnectWatcher();
    }
}
