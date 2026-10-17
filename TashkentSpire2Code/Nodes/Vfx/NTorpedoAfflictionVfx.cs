using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Afflictions;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>
/// Native affliction overlay: one shared torpedo texture, six foam bubbles and
/// five short-lived splash droplets. No emitters, gameplay RNG or awaited VFX.
/// NCard owns and removes this overlay when its model/affliction changes or it
/// returns to the pool; combat end also stops it immediately.
/// </summary>
public partial class NTorpedoAfflictionVfx : Control
{
    public const string ScenePath = "res://scenes/cards/overlays/afflictions/torpedo_affliction.tscn";

    private const float AppliedDuration = 0.55f;
    private const float LaunchDuration = 0.3f;
    private const float RedrawInterval = 1f / 30f;
    private const float TorpedoOpacity = 0.55f;
    private static readonly Vector2 TorpedoPosition = new(0f, 170f);
    private static readonly Vector2 TorpedoSize = new(130f, 43.3333f);
    private static readonly Color Foam = new(0.91f, 0.94f, 1f);
    private static readonly Color Lavender = new(0.64f, 0.61f, 0.88f);
    private static readonly Color Gold = new(1f, 0.85f, 0.56f);
    private static readonly StringName OverlayOrderOwner = "TashkentTorpedoOverlayOrderOwner";
    private static readonly StringName OverlayOrderIndex = "TashkentTorpedoOverlayOrderIndex";

    // Keep the glow on the frame, outside the description and below the cost/title.
    private static readonly Vector2[] LeftEdge =
    [
        new(-141f, -116f), new(-145f, -80f), new(-146f, -22f),
        new(-146f, 52f), new(-145f, 145f), new(-141f, 181f),
        new(-130f, 196f), new(-93f, 199f)
    ];
    private static readonly Vector2[] RightEdge =
    [
        new(141f, -116f), new(145f, -80f), new(146f, -22f),
        new(146f, 52f), new(145f, 145f), new(141f, 181f),
        new(130f, 196f), new(93f, 199f)
    ];

    [Export] public Texture2D? TorpedoTexture { get; set; }

    private NCard? _cardNode;
    private Node? _overlayContainer;
    private int _originalOverlayIndex = -1;
    private float _elapsed;
    private float _redrawElapsed;
    private float _pulseElapsed = AppliedDuration;
    private float _opacity;
    private bool _launching;

    public static IEnumerable<string> AssetPaths => [ScenePath];

    public override void _EnterTree()
    {
        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        for (Node? ancestor = GetParent(); ancestor != null; ancestor = ancestor.GetParent())
        {
            if (ancestor is NCard cardNode)
            {
                _cardNode = cardNode;
                Node? description = cardNode.GetNodeOrNull<Control>("%DescriptionLabel");
                if (description != null)
                {
                    PlaceBehindText(cardNode.OverlayContainer, description);
                }
                break;
            }
        }
        RefreshVisibility();
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        RestoreOverlayOrder();
        _cardNode = null;
    }

    public override void _Process(double delta)
    {
        RefreshVisibility();
        if (!IsVisibleInTree())
        {
            return;
        }

        float step = (float)delta;
        _elapsed += step;
        _pulseElapsed += step;
        _opacity = Mathf.MoveToward(_opacity, 1f, step / 0.25f);
        _redrawElapsed += step;
        if (_redrawElapsed >= RedrawInterval)
        {
            _redrawElapsed %= RedrawInterval;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        float duration = _launching ? LaunchDuration : AppliedDuration;
        float progress = Mathf.Clamp(_pulseElapsed / duration, 0f, 1f);
        float flash = Mathf.Pow(1f - progress, 2f);
        float breathing = 0.5f + 0.5f * Mathf.Sin(_elapsed * 2.1f);

        DrawFrameEdge(LeftEdge, breathing, flash);
        DrawFrameEdge(RightEdge, breathing, flash);
        DrawBubbles(flash);

        Vector2 center = TorpedoPosition + new Vector2(
            Mathf.Sin(_elapsed * 1.4f) * 3f,
            Mathf.Sin(_elapsed * 1.9f) * 2f);
        float alpha = _opacity * TorpedoOpacity;
        if (_launching && progress < 1f)
        {
            // A short local launch cue stays in the card's bottom strip. The combat
            // TorpedoPower visual handles the actual projectile and target.
            center.X += Mathf.Sin(progress * Mathf.Pi) * 14f;
            alpha *= 1f - 0.6f * Mathf.Sin(progress * Mathf.Pi);
        }

        DrawWake(center, flash);
        DrawSetTransform(center, Mathf.Sin(_elapsed * 1.7f) * 0.025f);
        if (TorpedoTexture != null)
        {
            DrawTextureRect(TorpedoTexture, new Rect2(-TorpedoSize * 0.5f, TorpedoSize),
                false, new Color(1f, 1f, 1f, alpha));
        }
        DrawSetTransform(Vector2.Zero);

        if (progress < 1f)
        {
            DrawSplash(center, progress, flash);
        }
    }

    public static void PlayOnCard(CardModel card, bool launching)
    {
        if (TestMode.IsOn)
        {
            return;
        }

        NCard? cardNode = NCard.FindOnTable(card);
        if (cardNode == null || !cardNode.IsNodeReady())
        {
            return;
        }

        // The official overlay layer is recreated on AfflictionChanged, so no
        // model-to-node cache or event subscription is needed for each card.
        foreach (Node child in cardNode.OverlayContainer.GetChildren())
        {
            if (child is NTorpedoAfflictionVfx vfx && !vfx.IsQueuedForDeletion())
            {
                vfx.PlayPulse(launching);
                break;
            }
        }
    }

    public void PlayPulse(bool launching)
    {
        _launching = launching;
        _pulseElapsed = 0f;
        _opacity = 1f;
        QueueRedraw();
    }

    private void RefreshVisibility()
    {
        if (_cardNode != null && GodotObject.IsInstanceValid(_cardNode))
        {
            Visible = _cardNode.Visibility == ModelVisibility.Visible
                && _cardNode.Model?.Affliction is TorpedoAffliction;
        }
    }

    private void PlaceBehindText(Node overlayContainer, Node description)
    {
        Node? parent = overlayContainer.GetParent();
        if (parent == null || parent != description.GetParent())
        {
            return;
        }

        // A pooled card can be reused before its previous overlay's queued
        // deletion runs. Transfer ownership so that old overlay cannot restore
        // the order underneath the new one at the end of the frame.
        bool inheritedOrder = overlayContainer.HasMeta(OverlayOrderOwner)
            && overlayContainer.HasMeta(OverlayOrderIndex);
        if (!inheritedOrder && overlayContainer.GetIndex() <= description.GetIndex())
        {
            return;
        }

        // Keep all card items at their native ZIndex. Raising only the text
        // could make one card's description bleed over neighbouring hand cards.
        // Instead, draw the native affliction container after the frame but
        // before the description, so its text/shadow always cover the VFX.
        _overlayContainer = overlayContainer;
        _originalOverlayIndex = inheritedOrder
            ? overlayContainer.GetMeta(OverlayOrderIndex).AsInt32()
            : overlayContainer.GetIndex();
        overlayContainer.SetMeta(OverlayOrderIndex, _originalOverlayIndex);
        overlayContainer.SetMeta(OverlayOrderOwner, unchecked((long)GetInstanceId()));
        if (overlayContainer.GetIndex() > description.GetIndex())
        {
            parent.MoveChild(overlayContainer, description.GetIndex());
        }
    }

    private void RestoreOverlayOrder()
    {
        if (_originalOverlayIndex >= 0 && _overlayContainer != null
            && GodotObject.IsInstanceValid(_overlayContainer)
            && _overlayContainer.HasMeta(OverlayOrderOwner)
            && _overlayContainer.GetMeta(OverlayOrderOwner).AsInt64() == unchecked((long)GetInstanceId()))
        {
            Node? parent = _overlayContainer.GetParent();
            if (parent != null && !parent.IsQueuedForDeletion()
                && (_cardNode == null || !_cardNode.IsQueuedForDeletion()))
            {
                parent.MoveChild(_overlayContainer, Mathf.Min(_originalOverlayIndex, parent.GetChildCount() - 1));
            }
            _overlayContainer.RemoveMeta(OverlayOrderOwner);
            _overlayContainer.RemoveMeta(OverlayOrderIndex);
        }
        _overlayContainer = null;
        _originalOverlayIndex = -1;
    }

    private void DrawFrameEdge(Vector2[] points, float breathing, float flash)
    {
        DrawPolyline(points, WithAlpha(Lavender, (0.055f + flash * 0.1f) * _opacity), 8f, true);
        DrawPolyline(points, WithAlpha(Foam, (0.17f + breathing * 0.11f + flash * 0.32f) * _opacity), 1.5f, true);
    }

    private void DrawBubbles(float flash)
    {
        for (int i = 0; i < 6; i++)
        {
            float phase = Mathf.PosMod(_elapsed * 0.19f + i * 0.173f, 1f);
            float side = i % 2 == 0 ? -1f : 1f;
            Vector2 position = new(side * (140f + Mathf.Sin(phase * Mathf.Tau + i) * 2f),
                Mathf.Lerp(171f, -113f, phase));
            float alpha = Mathf.Sin(phase * Mathf.Pi) * (0.28f + flash * 0.3f) * _opacity;
            float radius = 3.2f + (i % 3) * 1.7f;
            DrawArc(position, radius, 0f, Mathf.Tau, 12, WithAlpha(Foam, alpha), 1.5f, true);
            DrawCircle(position + new Vector2(-radius * 0.3f, -radius * 0.3f), 1.2f,
                WithAlpha(Foam, alpha * 0.8f));
        }
    }

    private void DrawWake(Vector2 center, float flash)
    {
        // Foam ellipses, not a solid coloured trajectory.
        DrawSetTransform(center - new Vector2(TorpedoSize.X * 0.47f, 0f), 0f, new Vector2(1.5f, 0.6f));
        for (int i = 0; i < 3; i++)
        {
            float phase = Mathf.PosMod(_elapsed * 0.6f + i / 3f, 1f);
            DrawArc(new Vector2(-phase * 16f, 0f), 3f + phase * 5f,
                Mathf.Pi * 0.5f, Mathf.Pi * 1.5f, 10,
                WithAlpha(Foam, (1f - phase) * (0.35f + flash * 0.35f) * _opacity), 1.2f, true);
        }
        DrawSetTransform(Vector2.Zero);
    }

    private void DrawSplash(Vector2 center, float progress, float flash)
    {
        // Flatten the splash and direct it downwards: none of the burst should
        // climb back into the description now that the torpedo sits below it.
        DrawSetTransform(center, 0f, new Vector2(1f, 0.28f));
        DrawArc(Vector2.Zero, 11f + progress * 27f, 0f, Mathf.Pi, 20,
            WithAlpha(Foam, flash * 0.65f), 1.8f, true);
        for (int i = 0; i < 5; i++)
        {
            float angle = 0.3f + i * (Mathf.Pi - 0.6f) / 4f;
            Vector2 direction = Vector2.FromAngle(angle);
            Vector2 drop = direction * (11f + progress * 34f);
            DrawCircle(drop, 1.5f + flash * 1.2f, WithAlpha(Foam, flash * 0.8f));
        }
        DrawSetTransform(Vector2.Zero);
        Vector2 glint = center + new Vector2(29f, -4f);
        DrawLine(glint - new Vector2(4f, 0f), glint + new Vector2(4f, 0f), WithAlpha(Gold, flash), 1.5f, true);
        DrawLine(glint - new Vector2(0f, 4f), glint + new Vector2(0f, 4f), WithAlpha(Gold, flash), 1.5f, true);
    }

    private static Color WithAlpha(Color color, float alpha) => new(color, alpha);

    private void OnCombatEnded(MegaCrit.Sts2.Core.Rooms.CombatRoom _)
    {
        Hide();
        SetProcess(false);
    }
}
