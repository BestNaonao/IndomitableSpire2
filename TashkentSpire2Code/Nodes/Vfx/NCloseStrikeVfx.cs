using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>Visual-only close-range strike. Never moves the creature, UI, or distance state.</summary>
public partial class NCloseStrikeVfx : Node
{
    private const float ReturnSpeed = 750f;
    private NCreature _attacker = null!;
    private NCreature _target = null!;
    private Node2D _body = null!;
    private Vector2 _localOffset;
    private Vector2 _impactOffset;
    private float _age;
    private float _approachDuration;
    private bool _charged;
    private bool _impacted;
    private bool _stopped;
    private bool _playingRetreat;
    private bool _changingAnimation;
    private MegaSprite? _spine;
    private Callable _animationStarted;

    // These two cards own this callback; do not use this helper to overwrite an
    // unrelated card's BeforeDamage hook. The normal Attack trigger/delay remains intact.
    public static AttackCommand Attach(AttackCommand command, Creature attacker, Creature target, bool charged)
    {
        NCloseStrikeVfx? effect = null;
        return command.WithAttackerFx(() =>
        {
            effect = Begin(attacker, target, charged);
            // Begin attaches its controller to the creature, not the shared VFX container.
            return null;
        }).BeforeDamage(() =>
        {
            if (GodotObject.IsInstanceValid(effect) && !effect!._stopped)
                effect.Impact();
            return Task.CompletedTask;
        });
    }

    private static NCloseStrikeVfx? Begin(Creature attacker, Creature target, bool charged)
    {
        if (TestMode.IsOn || attacker.Player?.Character is not TashkentCharacter
            || !attacker.IsAlive || !target.IsAlive || !CombatManager.Instance.IsInProgress
            || CombatManager.Instance.IsEnding)
            return null;
        NCombatRoom? room = NCombatRoom.Instance;
        NCreature? owner = room?.GetCreatureNode(attacker);
        NCreature? enemy = room?.GetCreatureNode(target);
        if (owner == null || enemy == null || !GodotObject.IsInstanceValid(owner.Body))
            return null;

        // A rapid replay replaces its predecessor, rather than accumulating offsets.
        foreach (NCloseStrikeVfx previous in owner.GetChildren().OfType<NCloseStrikeVfx>())
            previous.Stop();
        var effect = new NCloseStrikeVfx
        {
            Name = "TashkentCloseStrikeVfx", _attacker = owner, _target = enemy,
            _body = owner.Body, _charged = charged,
            _approachDuration = SaveManager.Instance.PrefsSave.FastMode switch
            {
                FastModeType.Fast => Math.Min(attacker.Player.Character.AttackAnimDelay * 0.5f, 0.25f),
                FastModeType.Instant => 0f,
                _ => attacker.Player.Character.AttackAnimDelay
            }
        };
        owner.AddChildSafely(effect);
        return effect;
    }

    public override void _EnterTree() => CombatManager.Instance.CombatEnded += OnCombatEnded;

    private Vector2 ContactOffset()
    {
        float distance = _target.VfxSpawnPosition.X - _attacker.VfxSpawnPosition.X;
        float direction = Mathf.Sign(distance);
        float ownerWidth = (_attacker.Visuals.Bounds.GetGlobalTransform().X * _attacker.Visuals.Bounds.Size.X).Length();
        float targetWidth = (_target.Visuals.Bounds.GetGlobalTransform().X * _target.Visuals.Bounds.Size.X).Length();
        float gap = Math.Max(85f, ownerWidth * 0.3f + targetWidth * 0.35f);
        return new Vector2(direction * Math.Max(0f, Mathf.Abs(distance) - gap),
            Mathf.Clamp(_target.GlobalPosition.Y - _attacker.GlobalPosition.Y, -100f, 100f));
    }

    private void Impact()
    {
        if (!CanContinue() || !GodotObject.IsInstanceValid(_target) || !_target.Entity.IsAlive)
        {
            Stop();
            return;
        }
        _impactOffset = ContactOffset();
        SetWorldOffset(_impactOffset);
        _impacted = true;
        _age = 0f;

        // All four Tashkent skins have their attack/action key at 0.2s. Seek only
        // this attack entry: fast mode hits at the same pose without a new wait,
        // global time scaling, or a delayed trigger overwriting victory/death.
        using var track = _attacker.SpineAnimation.GetCurrentTrack();
        if (track?.GetAnimationName() == "attack")
            track.SetTrackTime(0.2f);

        if (_spine == null && _attacker.Visuals.SpineBody is { } spine)
        {
            _animationStarted = Callable.From<GodotObject, GodotObject, GodotObject>(OnAnimationStarted);
            if (spine.ConnectAnimationStarted(_animationStarted) == Error.Ok)
                _spine = spine;
        }
    }

    public override void _Process(double delta)
    {
        if (!CanContinue())
        {
            Stop();
            return;
        }
        _age += Math.Max(0f, (float)delta);
        if (_impacted)
        {
            // The vanilla heavy-blunt burst has 0.2s of visual anticipation.
            // Stay beside the enemy through that burst, without delaying damage.
            float hold = _charged ? 0.23f : 0.21f;
            if (_age < hold)
                return;
            if (!_playingRetreat && _spine?.HasAnimation(TashkentCharacter.RetreatAnimationName) == true)
            {
                // Ignore only our own start signal. Any later animation start,
                // including another move_left or attack, owns the model instead.
                _changingAnimation = true;
                try
                {
                    _attacker.SetAnimationTrigger(TashkentCharacter.RetreatAnimationTrigger);
                    _playingRetreat = true;
                }
                finally
                {
                    _changingAnimation = false;
                }
            }
            Vector2 remaining = _impactOffset.MoveToward(Vector2.Zero,
                ReturnSpeed * (_age - hold));
            SetWorldOffset(remaining);
            if (remaining.IsZeroApprox())
                Stop(returnToIdle: true);
            return;
        }
        if (!GodotObject.IsInstanceValid(_target) || !_target.Entity.IsAlive || _age > 1.25f)
        {
            Stop();
            return;
        }
        Vector2 contact = ContactOffset();
        float duration = Math.Max(0.001f, _approachDuration);
        float windup = duration * (_charged ? 0.22f : 0.1f);
        Vector2 anticipation = new(-Mathf.Sign(contact.X) * (_charged ? 18f : 9f), 0f);
        if (_age < windup)
        {
            SetWorldOffset(anticipation * (_age / windup));
            return;
        }
        float dash = Mathf.Clamp((_age - windup) / (duration - windup), 0f, 1f);
        float rush = 1f - Mathf.Pow(1f - dash, 3f);
        SetWorldOffset(anticipation.Lerp(contact, rush)
            + new Vector2(0f, -12f * Mathf.Sin(dash * Mathf.Pi)));
    }

    private bool CanContinue() => !_stopped && IsInsideTree()
        && GodotObject.IsInstanceValid(_attacker) && _attacker.Entity.IsAlive
        && GodotObject.IsInstanceValid(_body) && _attacker.Body == _body
        && CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding
        && HasExpectedAnimation();

    private bool HasExpectedAnimation()
    {
        string? current = _attacker.SpineAnimation.GetCurrentAnimationName();
        return current == null || current == (_playingRetreat ? TashkentCharacter.RetreatAnimationName : "attack");
    }

    private void OnAnimationStarted(GodotObject _, GodotObject __, GodotObject ___)
    {
        if (!_changingAnimation)
            Stop();
    }

    private void DisconnectAnimationWatcher()
    {
        if (_spine == null)
            return;
        if (GodotObject.IsInstanceValid(_body))
            _spine.DisconnectAnimationStarted(_animationStarted);
        _spine = null;
    }

    private void SetWorldOffset(Vector2 worldOffset)
    {
        if (!GodotObject.IsInstanceValid(_body))
            return;
        Vector2 local = _body.GetParent() is CanvasItem parent
            ? parent.GetGlobalTransform().AffineInverse().BasisXform(worldOffset) : worldOffset;
        // Remove only our own additive offset, preserving other layout/visual changes.
        _body.Position += local - _localOffset;
        _localOffset = local;
    }

    private void OnCombatEnded(CombatRoom _) => Stop();

    private void Stop(bool returnToIdle = false)
    {
        if (_stopped)
            return;
        _stopped = true;
        DisconnectAnimationWatcher();
        SetWorldOffset(Vector2.Zero);
        SetProcess(false);
        if (returnToIdle && _playingRetreat && GodotObject.IsInstanceValid(_attacker)
            && _attacker.IsInsideTree() && _attacker.Entity.IsAlive
            && CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding
            && _attacker.SpineAnimation.GetCurrentAnimationName() == TashkentCharacter.RetreatAnimationName)
            _attacker.SetAnimationTrigger(CreatureAnimator.idleTrigger);
        this.QueueFreeSafely();
    }

    public override void _ExitTree()
    {
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        DisconnectAnimationWatcher();
        SetWorldOffset(Vector2.Zero);
    }
}
