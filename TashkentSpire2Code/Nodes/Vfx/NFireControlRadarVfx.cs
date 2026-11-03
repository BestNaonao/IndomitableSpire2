using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>A large radar behind the resolving card, independent of pooled card lifetime.</summary>
public partial class NFireControlRadarVfx : Node2D
{
    public const string RadarPath = "res://TashkentSpire2/images/vfx/mark/azurlane_radar_grid.png";
    public const string SweepPath = "res://TashkentSpire2/images/vfx/mark/azurlane_radar_sweep.png";
    public const string SweepShaderPath = "res://TashkentSpire2/shaders/fire_control_radar_sweep.gdshader";
    private const float Duration = 1.2f;

    private Creature? _caster;
    private Creature? _target;
    private CardModel? _card;
    private NCard? _cardNode;
    private Sprite2D _grid = null!;
    private Sprite2D _sweep = null!;
    private float _elapsed;
    private float _diameter;
    private bool _cardDeparted;

    public static void Play(CardModel card, Creature target)
    {
        if (!NMarkTargetVfx.CanPlay(card.Owner.Creature) || !target.IsAlive)
            return;

        NMarkTargetVfx.PlayFireControl(target);
        // Use the exact resolving instance, never a hand/preview copy or the
        // caster as a fallback. Remote plays without a visible card still lock.
        var ui = NCombatRoom.Instance?.Ui;
        NCard? cardNode = ui?.GetCardFromPlayContainer(card);
        if (ui == null || cardNode == null || !cardNode.IsNodeReady()
            || !cardNode.IsVisibleInTree() || card.Pile?.Type != PileType.Play)
            return;

        NFireControlRadarVfx? vfx = ui.PlayContainer.GetChildren().OfType<NFireControlRadarVfx>()
            .FirstOrDefault(node => node._card == card && !node.IsQueuedForDeletion());
        if (vfx == null)
        {
            vfx = new NFireControlRadarVfx { Name = "TashkentFireControlRadarVfx" };
            // Sibling of pooled cards, below them: no text obstruction and no
            // attachment to a card that can be returned to the pool mid-effect.
            ui.PlayContainer.AddChildSafely(vfx);
            ui.PlayContainer.MoveChildSafely(vfx, 0);
        }
        vfx._caster = card.Owner.Creature;
        vfx._card = card;
        vfx._cardNode = cardNode;
        vfx._target = target;
        vfx._elapsed = 0f;
        vfx._cardDeparted = false;
        vfx.UpdateCardAnchor();
    }

    public override void _EnterTree() => CombatManager.Instance.CombatEnded += OnCombatEnded;

    public override void _Ready()
    {
        CanvasItemMaterial additive = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _grid = new Sprite2D
        {
            Name = "RadarGrid", Texture = PreloadManager.Cache.GetTexture2D(RadarPath), Material = additive
        };
        AddChild(_grid);
        _sweep = new Sprite2D
        {
            Name = "RadarSweep", Texture = PreloadManager.Cache.GetTexture2D(SweepPath),
            Material = new ShaderMaterial { Shader = PreloadManager.Cache.GetAsset<Shader>(SweepShaderPath) }
        };
        AddChild(_sweep);
        Modulate = new Color(1f, 1f, 1f, 0f);
    }

    public override void _Process(double delta)
    {
        if (_caster == null || !NMarkTargetVfx.CanPlay(_caster) || _target == null || !_target.IsAlive)
        {
            StopImmediately();
            return;
        }

        if (!_cardDeparted && !UpdateCardAnchor())
        {
            // Complete the independent scan at its last play-area position,
            // without following the discard/exhaust flight or recycled card.
            _cardDeparted = true;
        }
        _elapsed += Math.Max(0f, (float)delta);
        if (_elapsed >= Duration)
        {
            StopImmediately();
            return;
        }

        float progress = _elapsed / Duration;
        float entrance = Mathf.Clamp(_elapsed / 0.1f, 0f, 1f);
        float fade = Mathf.Clamp((Duration - _elapsed) / 0.28f, 0f, 1f);
        float diameter = _diameter * (0.82f + 0.18f * (1f - Mathf.Pow(1f - progress, 3f)));
        _grid.Scale = Vector2.One * (diameter / _grid.Texture.GetWidth());
        _grid.Modulate = new Color(0.9f, 1f, 1f, 0.96f);
        // Stay in play-container coordinates, also when combat UI and creatures
        // live on different canvas layers. No cross-canvas targeting line.
        _sweep.Rotation = -Mathf.Pi * 0.5f + progress * Mathf.Tau * 0.8f;
        _sweep.Scale = new Vector2(diameter / _sweep.Texture.GetWidth(), diameter / _sweep.Texture.GetHeight());
        _sweep.Modulate = new Color(0.45f, 0.82f, 1f, 0.7f);
        Modulate = new Color(1f, 1f, 1f, entrance * fade);
    }

    private bool UpdateCardAnchor()
    {
        if (_cardNode == null || !GodotObject.IsInstanceValid(_cardNode)
            || _cardNode.IsQueuedForDeletion() || _cardNode.Model != _card
            || _card?.Pile?.Type != PileType.Play || _cardNode.GetParent() != GetParent()
            || !_cardNode.IsVisibleInTree())
            return false;

        var parent = (CanvasItem)GetParent();
        Transform2D cardToContainer = parent.GetGlobalTransform().AffineInverse() * _cardNode.GetGlobalTransform();
        Position = cardToContainer * (_cardNode.Size * 0.5f);
        float diagonal = (cardToContainer * _cardNode.Size - cardToContainer * Vector2.Zero).Length();
        _diameter = Mathf.Clamp(diagonal * 1.8f, 660f, 880f);
        return true;
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        _caster = null;
        _target = null;
        _card = null;
        _cardNode = null;
    }

    private void OnCombatEnded(CombatRoom _) => StopImmediately();

    private void StopImmediately()
    {
        Visible = false;
        SetProcess(false);
        this.QueueFreeSafely();
    }
}
