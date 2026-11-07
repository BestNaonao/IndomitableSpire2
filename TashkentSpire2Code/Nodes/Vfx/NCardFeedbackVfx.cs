using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>Ammo-load arcs only; never adds effects to exhausted cards or lives on pooled cards.</summary>
public partial class NCardFeedbackVfx : Node2D
{
    public const string ArcSmallPath = "res://TashkentSpire2/images/vfx/card_feedback/azurlane_load_arc_2x3.png";
    public const string ArcWidePath = "res://TashkentSpire2/images/vfx/card_feedback/azurlane_load_arc_6x2.png";
    public const string MaskShaderPath = "res://TashkentSpire2/shaders/azurlane_feedback_mask.gdshader";
    public static IEnumerable<string> AssetPaths => [ArcSmallPath, ArcWidePath, MaskShaderPath];
    private const int Capacity = 32;
    private const float OuterArcDuration = 0.68f;
    private const float InnerArcDuration = 0.34f;
    private struct Burst
    {
        public bool Active;
        public float Age;
        public NCard? Card;
        public CardModel? Model;
        public Transform2D Anchor;
    }
    private readonly Burst[] _bursts = new Burst[Capacity];
    private readonly Sprite2D[] _drawLayers = new Sprite2D[2];
    private Texture2D[] _textures = [];
    private int _next;
    private float _redrawTime;

    public static void PlayLoad(CardModel card)
    {
        if (TestMode.IsOn || !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding
            || card.Pile?.Type is not (PileType.Hand or PileType.Play))
            return;
        var ui = NCombatRoom.Instance?.Ui;
        NCard? node = NCard.FindOnTable(card);
        if (ui == null || node == null || !node.IsNodeReady() || !node.IsVisibleInTree()
            || node.Visibility != ModelVisibility.Visible || node.Model != card)
            return;
        if (node.Body.GetNodeOrNull<TextureRect>("TashkentAmmoIcon") is not { Visible: true })
            return;
        var renderer = ui.GetChildren().OfType<NCardFeedbackVfx>()
            .FirstOrDefault(n => !n.IsQueuedForDeletion());
        if (renderer == null)
        {
            renderer = new NCardFeedbackVfx { Name = "TashkentCardFeedback" };
            ui.AddChildSafely(renderer);
        }
        ui.MoveChildSafely(renderer, ui.GetChildCount() - 1);
        // Multiple loads of the same card coalesce into one confirmation, not a flash queue.
        int index = Array.FindIndex(renderer._bursts, b => b.Active && b.Model == card);
        if (index < 0)
        {
            index = renderer._next;
            renderer._next = (renderer._next + 1) % Capacity;
        }
        renderer._bursts[index] = new Burst { Active = true, Card = node, Model = card };
        renderer.UpdateAnchor(ref renderer._bursts[index]);
        renderer.SetProcess(true);
        renderer.RedrawLayers();
    }

    public override void _EnterTree() => CombatManager.Instance.CombatEnded += OnCombatEnded;

    public override void _Ready()
    {
        _textures = [PreloadManager.Cache.GetTexture2D(ArcSmallPath), PreloadManager.Cache.GetTexture2D(ArcWidePath)];
        for (int i = 0; i < _drawLayers.Length; i++)
        {
            var material = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>(MaskShaderPath) };
            material.SetShaderParameter("sheet_grid", i == 0 ? new Vector2(2, 3) : new Vector2(6, 2));
            var layer = new Sprite2D { Material = material };
            int layerIndex = i;
            layer.Draw += () => DrawLayer(layerIndex);
            AddChild(layer);
            _drawLayers[i] = layer;
        }
        SetProcess(false);
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
            ref Burst burst = ref _bursts[i];
            if (!burst.Active)
                continue;
            burst.Age += Math.Max(0f, (float)delta);
            if (burst.Age >= OuterArcDuration)
            {
                burst = default;
                continue;
            }
            // Never follow a discarded/recycled card or reveal a hidden remote hand.
            if (!ValidCard(burst) || burst.Model?.Pile?.Type is not (PileType.Hand or PileType.Play))
            {
                burst = default;
                continue;
            }
            UpdateAnchor(ref burst);
            active = true;
        }
        _redrawTime += (float)delta;
        if (_redrawTime >= 1f / 30f || !active)
        {
            _redrawTime = 0f;
            RedrawLayers();
        }
        if (!active)
            SetProcess(false);
    }

    private static bool ValidCard(Burst burst) => GodotObject.IsInstanceValid(burst.Card)
        && !burst.Card!.IsQueuedForDeletion() && burst.Card.IsInsideTree()
        && burst.Card.IsVisibleInTree() && burst.Card.Visibility == ModelVisibility.Visible
        && burst.Card.Model == burst.Model;

    private void UpdateAnchor(ref Burst burst)
    {
        if (!ValidCard(burst))
            return;
        Transform2D source = burst.Card!.GetGlobalTransform();
        if (burst.Card.Body.GetNodeOrNull<TextureRect>("TashkentAmmoIcon") is { } icon)
            source = icon.GetGlobalTransform() * new Transform2D(0f, icon.Size * 0.5f);
        burst.Anchor = GetGlobalTransform().AffineInverse() * source;
    }

    private void DrawLayer(int layer)
    {
        Sprite2D canvas = _drawLayers[layer];
        foreach (Burst burst in _bursts)
        {
            if (!burst.Active)
                continue;
            canvas.DrawSetTransformMatrix(burst.Anchor);
            float t = Mathf.Clamp(burst.Age / OuterArcDuration, 0f, 1f);
            float opacity = ArcOpacity(t);
            Vector2 grid = layer == 0 ? new(2, 3) : new(6, 2);
            int frame = Math.Min(layer == 0 ? 5 : 11, (int)(t * grid.X * grid.Y));
            Vector2 cell = _textures[layer].GetSize() / grid;
            float diameter = layer == 0 ? 184.6f : 200.2f;
            canvas.DrawSetTransformMatrix(burst.Anchor * new Transform2D(t * Mathf.Tau, Vector2.Zero));
            canvas.DrawTextureRectRegion(_textures[layer], new Rect2(Vector2.One * -diameter * 0.5f, Vector2.One * diameter),
                new Rect2(new Vector2(frame % (int)grid.X, frame / (int)grid.X) * cell, cell),
                new Color(1f, 1f, 1f, opacity));
            if (layer == 0 && burst.Age < InnerArcDuration)
            {
                // A separate inner arc: reuse the small sheet, not extra nodes,
                // and retain its center mask so the ammo digits stay readable.
                const float innerDiameter = 116f;
                float innerT = Mathf.Clamp(burst.Age / InnerArcDuration, 0f, 1f);
                int innerFrame = Math.Min(5, (int)(innerT * 6f));
                canvas.DrawSetTransformMatrix(burst.Anchor * new Transform2D(-innerT * Mathf.Tau, Vector2.Zero));
                canvas.DrawTextureRectRegion(_textures[layer],
                    new Rect2(Vector2.One * -innerDiameter * 0.5f, Vector2.One * innerDiameter),
                    new Rect2(new Vector2(innerFrame % 2, innerFrame / 2) * cell, cell),
                    new Color(1f, 1f, 1f, ArcOpacity(innerT) * 0.9f));
            }
        }
        canvas.DrawSetTransformMatrix(Transform2D.Identity);
    }

    private static float ArcOpacity(float t) => Mathf.Min(1f, t / 0.13f) * Mathf.Pow(1f - t, 0.75f);

    private void RedrawLayers()
    {
        foreach (Sprite2D layer in _drawLayers)
            layer.QueueRedraw();
    }

    private void OnCombatEnded(CombatRoom _) => Stop();
    private void Stop()
    {
        Array.Clear(_bursts);
        Visible = false;
        SetProcess(false);
        this.QueueFreeSafely();
    }
    public override void _ExitTree() => CombatManager.Instance.CombatEnded -= OnCombatEnded;
}
