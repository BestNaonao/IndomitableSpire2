using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>
/// A brilliant white flare and a bounded shower of falling white embers, using
/// the original AL_Glow01 from Azur Lane's zhaomingdan_baolu. Purely cosmetic.
/// </summary>
public partial class NIlluminationRoundVfx : Node2D
{
    public const string GlowPath = "res://TashkentSpire2/images/vfx/mark/azurlane_illumination_glow.png";
    public static IEnumerable<string> AssetPaths => [GlowPath];
    private const float Duration = 2.4f;
    private const float HeadClearance = 180f;
    private NCreature? _targetNode;
    private readonly Sprite2D[] _glows = new Sprite2D[2];
    private readonly Sprite2D[] _sparks = new Sprite2D[20];
    private float _elapsed;

    public static void Play(Creature target)
    {
        if (!NMarkTargetVfx.CanPlay(target))
            return;
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(target);
        if (node == null)
            return;

        var vfx = node.GetChildren().OfType<NIlluminationRoundVfx>()
            .FirstOrDefault(effect => !effect.IsQueuedForDeletion());
        if (vfx == null)
        {
            vfx = new NIlluminationRoundVfx { Name = "TashkentIlluminationRoundVfx" };
            node.AddChildSafely(vfx);
            // Like mark confirmation, render below health, intents and selection UI.
            Control? healthBar = node.GetNodeOrNull<Control>("HealthBar");
            if (healthBar != null && healthBar.GetParent() == node)
                node.MoveChildSafely(vfx, healthBar.GetIndex());
        }
        vfx._elapsed = 0f;
        vfx.UpdateHeadPosition();
    }

    public override void _EnterTree() => CombatManager.Instance.CombatEnded += OnCombatEnded;

    public override void _Ready()
    {
        _targetNode = GetParent() as NCreature;
        Texture2D texture = PreloadManager.Cache.GetTexture2D(GlowPath);
        CanvasItemMaterial additive = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        for (int i = 0; i < _glows.Length; i++)
        {
            _glows[i] = new Sprite2D { Name = $"Glow{i}", Texture = texture, Material = additive };
            AddChild(_glows[i]);
        }
        for (int i = 0; i < _sparks.Length; i++)
        {
            _sparks[i] = new Sprite2D { Name = $"LightParticle{i}", Texture = texture, Material = additive };
            AddChild(_sparks[i]);
        }
        Modulate = new Color(1f, 1f, 1f, 0f);
    }

    public override void _Process(double delta)
    {
        if (_targetNode == null || !GodotObject.IsInstanceValid(_targetNode)
            || !NMarkTargetVfx.CanPlay(_targetNode.Entity))
        {
            StopImmediately();
            return;
        }
        _elapsed += Math.Max(0f, (float)delta);
        if (_elapsed >= Duration)
        {
            StopImmediately();
            return;
        }

        UpdateHeadPosition();
        float envelope = Mathf.Clamp(_elapsed / 0.12f, 0f, 1f)
            * Mathf.Clamp((Duration - _elapsed) / 0.45f, 0f, 1f);
        Modulate = new Color(1f, 1f, 1f, envelope);
        // Separate a broad soft halo from the small overbright white core.
        // Additive HDR tint supplies the bright-light appearance even on the
        // compatibility renderer without depending on global bloom settings.
        float pulse = 1f + 0.035f * Mathf.Sin(_elapsed * 8f);
        _glows[0].Scale = Vector2.One * (260f * pulse / _glows[0].Texture.GetWidth());
        _glows[0].Modulate = new Color(1.3f, 1.4f, 1.5f, 0.92f);
        _glows[1].Scale = Vector2.One * (72f * pulse / _glows[1].Texture.GetWidth());
        _glows[1].Modulate = new Color(3f, 3f, 3f, 1f);
        for (int i = 0; i < _sparks.Length; i++)
        {
            // Fixed phase offsets avoid emitter allocation and gameplay RNG.
            // Each ember fades at the wrap, drifts sideways and accelerates down.
            float lifetime = 1.05f + (i % 4) * 0.08f;
            float age = Mathf.PosMod(_elapsed + i * 0.071f, lifetime);
            float progress = age / lifetime;
            float angle = i * 2.399963f;
            _sparks[i].Position = new Vector2(
                Mathf.Sin(angle) * (6f + age * 30f) + Mathf.Sin(age * 3f + angle) * 4f,
                8f + age * 65f + age * age * 55f);
            _sparks[i].Rotation = Mathf.Sin(angle) * 0.16f;
            _sparks[i].Scale = new Vector2(11f + i % 3 * 3f, 24f + age * 12f)
                / _sparks[i].Texture.GetWidth();
            _sparks[i].Modulate = new Color(2.2f, 2.2f, 2.2f, Mathf.Sin(progress * Mathf.Pi));
        }
    }

    private void UpdateHeadPosition()
    {
        if (_targetNode == null)
            return;
        Control bounds = _targetNode.Visuals.Bounds;
        Transform2D transform = bounds.GetGlobalTransform();
        Vector2 topLeft = transform * Vector2.Zero;
        Vector2 topRight = transform * new Vector2(bounds.Size.X, 0f);
        Vector2 bottomLeft = transform * new Vector2(0f, bounds.Size.Y);
        Vector2 bottomRight = transform * bounds.Size;
        // Derive the top from actual bounds, including flipped/scaled skins.
        float top = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        GlobalPosition = new Vector2(((topLeft + bottomRight) * 0.5f).X, top - HeadClearance);
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        _targetNode = null;
    }

    private void OnCombatEnded(CombatRoom _) => StopImmediately();

    private void StopImmediately()
    {
        Visible = false;
        SetProcess(false);
        this.QueueFreeSafely();
    }
}
