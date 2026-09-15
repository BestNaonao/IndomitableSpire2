using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Forms;
using MegaCrit.Sts2.Core.TestSupport;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>
/// A lightweight form effect made from two shared tide sprites and six reusable
/// splash sprites. Cosmetic timing is fixed and never consumes combat RNG.
/// </summary>
public partial class NPoseidonFormVfx : NFormVfx
{
    public const string ScenePath = "res://TashkentSpire2/scenes/vfx/poseidon_form_vfx.tscn";

    private const float SplashPeriod = 2.4f;
    private const float SplashLifetime = 0.82f;

    private static readonly Vector2[] SplashPositions =
    [
        new(-154f, 2f),
        new(-104f, -5f),
        new(-48f, 4f),
        new(51f, 2f),
        new(108f, -6f),
        new(158f, 3f)
    ];

    private static readonly float[] SplashPhaseOffsets = [0f, 0.8f, 1.6f, 0.4f, 1.2f, 2f];

    private Sprite2D _backTide = null!;
    private Sprite2D _frontTide = null!;
    private Sprite2D[] _splashes = [];
    private float _elapsed;
    private float _visibility;
    private float _triggerStrength;
    private bool _shouldFreeWhenHidden;

    public static IEnumerable<string> AssetPaths => [ScenePath];

    public static NPoseidonFormVfx? Create(Creature target)
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        Player? player = target.Player;
        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (player == null || creatureNode == null || creatureNode.Visuals.FormVfxHolder == null)
        {
            return null;
        }

        NPoseidonFormVfx vfx = PreloadManager.Cache.GetScene(ScenePath)
            .Instantiate<NPoseidonFormVfx>(PackedScene.GenEditState.Disabled);
        vfx.Initialize(player);
        creatureNode.Visuals.AddFormVfx(vfx);
        vfx.SetActive(true);
        return vfx;
    }

    public override void _Ready()
    {
        _backTide = GetNode<Sprite2D>("Tide/BackTide");
        _frontTide = GetNode<Sprite2D>("Tide/FrontTide");
        Node splashes = GetNode("Splashes");
        _splashes = splashes.GetChildren().OfType<Sprite2D>().ToArray();

        Modulate = new Color(1f, 1f, 1f, 0f);
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        float frameDelta = (float)delta;
        _elapsed += frameDelta;
        _triggerStrength = Mathf.MoveToward(_triggerStrength, 0f, frameDelta * 1.35f);
        _visibility = Mathf.MoveToward(_visibility, _isActive ? 1f : 0f, frameDelta * 2.8f);
        Modulate = new Color(1f, 1f, 1f, _visibility);

        AnimateTides();
        AnimateSplashes();

        if (_shouldFreeWhenHidden && _visibility <= 0.001f)
        {
            SetProcess(false);
            this.QueueFreeSafely();
        }
    }

    public override void OnEffectTriggered()
    {
        base.OnEffectTriggered();
        _triggerStrength = 1f;
    }

    public override void SetActive(bool isActive)
    {
        base.SetActive(isActive);
        _shouldFreeWhenHidden = !isActive;
        if (isActive)
        {
            SetProcess(true);
        }
    }

    private void AnimateTides()
    {
        float tide = 0.5f + Mathf.Sin(_elapsed * 1.75f) * 0.5f;
        float counterTide = 1f - tide;
        float surge = _triggerStrength * 0.035f;

        _backTide.Scale = new Vector2(0.255f + tide * 0.032f + surge, 0.095f + tide * 0.026f);
        _backTide.Position = new Vector2(Mathf.Sin(_elapsed * 0.68f) * 8f, -16f - tide * 5f);
        _backTide.Modulate = new Color(0.42f, 0.8f, 1f, 0.25f + tide * 0.17f);

        _frontTide.Scale = new Vector2(0.235f + counterTide * 0.038f + surge, 0.088f + counterTide * 0.024f);
        _frontTide.Position = new Vector2(Mathf.Sin(_elapsed * 0.61f + 2.1f) * 7f, 5f + tide * 3f);
        _frontTide.Modulate = new Color(0.68f, 0.94f, 1f, 0.34f + counterTide * 0.22f);
    }

    private void AnimateSplashes()
    {
        int count = Mathf.Min(_splashes.Length, SplashPositions.Length);
        for (int i = 0; i < count; i++)
        {
            Sprite2D splash = _splashes[i];
            float cycle = Mathf.PosMod(_elapsed + SplashPhaseOffsets[i], SplashPeriod);
            if (cycle >= SplashLifetime)
            {
                splash.Visible = false;
                continue;
            }

            splash.Visible = true;
            float progress = cycle / SplashLifetime;
            float arc = Mathf.Sin(progress * Mathf.Pi);
            float side = i < count / 2 ? -1f : 1f;
            float burst = 1f + _triggerStrength * 0.45f;
            float baseScale = 0.046f + (i % 3) * 0.006f;

            splash.Position = SplashPositions[i] + new Vector2(
                side * Mathf.Sin(progress * Mathf.Pi) * (7f + i % 2 * 4f),
                -arc * (34f + i % 3 * 8f) * burst);
            splash.Scale = Vector2.One * baseScale * (0.62f + arc * 0.48f) * burst;
            splash.Rotation = Mathf.DegToRad(side * (8f + arc * 11f));
            splash.Modulate = new Color(0.72f, 0.94f, 1f,
                Mathf.Pow(arc, 0.7f) * (0.42f + _triggerStrength * 0.3f));
        }
    }
}
