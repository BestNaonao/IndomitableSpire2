using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Nodes;

/// <summary>One lightweight, scene-owned controller per animated Tashkent, including remote players.</summary>
public partial class NTashkentIdleAnimation : Node
{
    internal const string TriggerPrefix = "TashkentIdle_";
    internal static readonly string[] AnimationNames = ["stand", "stand2"];
    private const string ControllerName = "TashkentIdleAnimation";
    private const double PollSeconds = 0.2d;
    private readonly TashkentIdleClock _clock = new();
    private NCreature? _creature;
    private Control? _preview;
    private Func<bool>? _previewInteractable;
    private MegaSprite _spine = null!;
    private MegaAnimationState? _state;
    private string[] _available = [];
    private string? _ownedAnimation;
    private int _nextAnimation;
    private double _pollElapsed;

    public static void AttachCombat(NCreature creature)
    {
        if (TestMode.IsOn || creature.Entity.Player?.Character is not TashkentCharacter
            || creature.Visuals.SpineBody is not { } spine
            || creature.GetNodeOrNull<Node>(ControllerName) != null)
            return;
        creature.AddChildSafely(new NTashkentIdleAnimation
        {
            Name = ControllerName, _creature = creature, _spine = spine
        });
    }

    public static NTashkentIdleAnimation AttachPreview(Node2D visual, MegaSprite spine,
        Control panel, Func<bool> interactable)
    {
        var controller = new NTashkentIdleAnimation
        {
            Name = ControllerName, _spine = spine, _preview = panel,
            _previewInteractable = interactable
        };
        visual.AddChildSafely(controller);
        return controller;
    }

    public static void NotifyActivity(Player player)
    {
        if (TestMode.IsOn || player.Character is not TashkentCharacter)
            return;
        NCombatRoom.Instance?.GetCreatureNode(player.Creature)
            ?.GetNodeOrNull<NTashkentIdleAnimation>(ControllerName)?.ResetActivity();
    }

    public override void _Ready()
    {
        this.RunWhenSpineReady(_spine, state =>
        {
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion())
                return;
            _state = state;
            _available = AnimationNames.Where(_spine.HasAnimation).ToArray();
            if (_preview != null)
                state.SetAnimation("normal");
            if (_available.Length == 0)
                SetProcess(false);
        });
    }

    public void ResetActivity()
    {
        _clock.Reset();
        _pollElapsed = 0d;
        // Only cancel our own pose; never overwrite an attack, hit, movement or victory.
        if (_ownedAnimation != null && CurrentAnimation() == _ownedAnimation)
        {
            if (_creature != null)
                _creature.SetAnimationTrigger("Idle");
            else
                _state?.SetAnimation("normal");
        }
        _ownedAnimation = null;
    }

    public override void _Process(double delta)
    {
        if (_state == null || _available.Length == 0)
            return;
        // Poll native Spine only five times per second, and don't count long background stalls.
        _pollElapsed += Math.Min(delta, PollSeconds);
        if (_pollElapsed < PollSeconds)
            return;
        double elapsed = _pollElapsed;
        _pollElapsed = 0d;

        if (!CanIdle())
        {
            ResetActivity();
            return;
        }
        string? current = CurrentAnimation();
        if (!_clock.Advance(elapsed, current == "normal"))
            return;

        _ownedAnimation = _available[_nextAnimation];
        _nextAnimation = (_nextAnimation + 1) % _available.Length;
        if (_creature != null)
            _creature.SetAnimationTrigger(TriggerPrefix + _ownedAnimation);
        else
        {
            _state.SetAnimation(_ownedAnimation, loop: false);
            _state.AddAnimation("normal", loop: true);
        }
    }

    private bool CanIdle()
    {
        if (_creature == null)
            return GodotObject.IsInstanceValid(_preview) && _preview!.IsVisibleInTree()
                && (_previewInteractable?.Invoke() ?? false);

        CombatManager combat = CombatManager.Instance;
        Player? player = _creature.Entity.Player;
        return _creature.IsVisibleInTree() && _creature.Entity.IsAlive
            && NCombatRoom.Instance?.GetCreatureNode(_creature.Entity) == _creature
            && combat.IsInProgress && !combat.IsEnding && !combat.IsPaused
            && player?.PlayerCombatState?.Phase == PlayerTurnPhase.Play
            && !combat.IsPlayerReadyToEndTurn(player)
            && RunManager.Instance.ActionQueueSet.IsEmpty
            && !RunManager.Instance.ActionExecutor.IsPaused;
    }

    private string? CurrentAnimation()
    {
        if (_state == null || !GodotObject.IsInstanceValid(_spine.BoundObject))
            return null;
        using var track = _state.GetCurrent(0);
        return track?.GetAnimationName();
    }
}
