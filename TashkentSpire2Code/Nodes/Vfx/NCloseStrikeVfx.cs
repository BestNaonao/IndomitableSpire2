using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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
    private const float ChargedWindupDuration = 0.68f;
    private const float ChargedCrouchDuration = 0.2f;
    private const float ChargedRushDuration = 0.16f;
    private const float ChargedAttackDelay = ChargedWindupDuration + ChargedRushDuration;
    private const float CriticalAttackDelay = 0.08f;
    private const float ChargedWindupRatio = ChargedWindupDuration / ChargedAttackDelay;
    private const float CriticalHopDurationMultiplier = 2f;
    private const string CrouchAnimation = "tashkent_charge_crouch";
    private const int CrouchTrack = 1;
    private NCreature _attacker = null!;
    private NCreature _target = null!;
    private Node2D _body = null!;
    private Vector2 _localOffset;
    private Vector2 _impactOffset;
    private float _age;
    private float _approachDuration;
    private CrouchAnimationState? _crouchState;
    private bool _crouchOwned, _crouchUnavailable;
    private bool _charged;
    private bool _impacted;
    private bool _stopped;
    private bool _playingRetreat;
    private bool _changingAnimation;
    private MegaSprite? _spine;
    private Callable _animationStarted;

    // These two cards own this callback; do not use this helper to overwrite an
    // unrelated card's BeforeDamage hook. Use the official Attack delay so every
    // client hits at the same cue, without awaiting a local presentation node.
    public static AttackCommand Attach(AttackCommand command, Creature attacker, Creature target, bool charged)
    {
        if (attacker.Player?.Character is TashkentCharacter)
        {
            command.WithAttackerAnim("Attack", charged ? ChargedAttackDelay : CriticalAttackDelay);
            if (charged)
            {
                // TriggerAnim caps its Fast wait at .25s. Complete only that
                // capped portion through the official command clock, so Fast
                // also gets twice the windup (.34s) and the same .08s rush.
                command.AfterAttackerAnim(() => Cmd.CustomScaledWait(
                    ChargedAttackDelay * 0.5f - 0.25f, 0f));
            }
        }
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
                FastModeType.Fast => (charged ? ChargedAttackDelay : CriticalAttackDelay) * 0.5f,
                FastModeType.Instant => 0f,
                _ => charged ? ChargedAttackDelay : CriticalAttackDelay
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
        SetChargePose(0f);
        SetWorldOffset(_impactOffset);
        _impacted = true;
        _age = 0f;

        // All four Tashkent skins have their attack/action key at 0.2s. Seek only
        // this attack entry: fast mode hits at the same pose without a new wait,
        // global time scaling, or a delayed trigger overwriting victory/death.
        AlignAttackPose(0.2f);

        WatchAnimationStarts();
    }

    private void WatchAnimationStarts()
    {
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
            // Charged strikes stay through the heavy-blunt anticipation;
            // critical strikes deliberately break away sooner for a snappy hop.
            float hold = _charged ? 0.23f : 0.1f;
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
                    if (!_charged)
                    {
                        using var track = _attacker.SpineAnimation.GetCurrentTrack();
                        if (track?.GetAnimationName() == TashkentCharacter.RetreatAnimationName)
                            track.SetTimeScale(0.5f);
                    }
                    _playingRetreat = true;
                }
                finally
                {
                    _changingAnimation = false;
                }
            }
            Vector2 remaining;
            if (_charged)
                remaining = _impactOffset.MoveToward(Vector2.Zero, ReturnSpeed * (_age - hold));
            else
            {
                // Only the return arc is doubled; hit timing/height stay unchanged.
                float hopDuration = Mathf.Clamp(_impactOffset.Length() / 2800f, 0.18f, 0.32f)
                    * CriticalHopDurationMultiplier;
                float progress = Mathf.Clamp((_age - hold) / hopDuration, 0f, 1f);
                float ease = 1f - Mathf.Pow(1f - progress, 2f);
                float height = Mathf.Clamp(_impactOffset.Length() * 0.1f, 35f, 80f);
                remaining = _impactOffset.Lerp(Vector2.Zero, ease)
                    + new Vector2(0f, -height * Mathf.Sin(progress * Mathf.Pi));
                if (progress >= 1f)
                    remaining = Vector2.Zero;
            }
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
        float windup = _charged ? duration * ChargedWindupRatio : 0f;
        // Foot planting is authored in the Spine clip. Moving the whole body
        // during anticipation would make those otherwise fixed feet slide.
        Vector2 anticipation = Vector2.Zero;
        // The first Attack trigger follows Begin in the same command. Start
        // watching on the first process frame, after that initial trigger.
        WatchAnimationStarts();
        if (_age < windup)
        {
            float pose = Mathf.Clamp(_age / Math.Min(windup, ChargedCrouchDuration), 0f, 1f);
            SetChargePose(pose);
            AlignAttackPose(0.06f);
            SetWorldOffset(anticipation * pose);
            return;
        }
        float dash = Mathf.Clamp((_age - windup) / (duration - windup), 0f, 1f);
        float rush = 1f - Mathf.Pow(1f - dash, 3f);
        SetChargePose(_charged ? 1f - rush : 0f);
        AlignAttackPose(Mathf.Lerp(_charged ? 0.06f : 0f, 0.2f, rush));
        SetWorldOffset(anticipation.Lerp(contact, rush)
            + new Vector2(0f, -12f * Mathf.Sin(dash * Mathf.Pi)));
    }

    private void AlignAttackPose(float time)
    {
        using var track = _attacker.SpineAnimation.GetCurrentTrack();
        if (track?.GetAnimationName() == "attack")
            track.SetTrackTime(time);
    }

    private void SetChargePose(float amount)
    {
        if (amount <= 0f)
        {
            ClearCrouch();
            return;
        }
        if (_crouchUnavailable || !_charged || _spine == null || !GodotObject.IsInstanceValid(_body))
            return;
        if (_crouchState == null)
        {
            // Do not steal another presentation's overlay track. Missing/new
            // custom skins still get the rush, without falling back to squash.
            if (!_spine.HasAnimation(CrouchAnimation))
            {
                _crouchUnavailable = true;
                return;
            }
            using var native = _spine.BoundObject.Call("get_animation_state");
            _crouchState = new CrouchAnimationState(native);
            GC.KeepAlive(_spine);
            using var existing = _crouchState.GetCurrent(CrouchTrack);
            if (existing != null)
            {
                // Borrowed from SpineSprite: this is the same native state used
                // by CreatureAnimator and NTashkentIdleAnimation, not our resource.
                _crouchState = null;
                _crouchUnavailable = true;
                return;
            }
            _changingAnimation = true;
            try
            {
                _crouchState.SetAnimation(CrouchAnimation, false, CrouchTrack);
                _crouchOwned = true;
            }
            finally { _changingAnimation = false; }
        }
        using var track = _crouchState.GetCurrent(CrouchTrack);
        if (track?.GetAnimationName() == CrouchAnimation)
        {
            track.SetMixDuration(0f);
            track.SetTimeScale(0f);
            track.SetTrackTime(Mathf.Clamp(amount, 0f, 1f) * ChargedCrouchDuration);
        }
    }

    private void ClearCrouch()
    {
        if (_crouchState == null)
            return;
        // Only remove our dedicated track, never the base attack or another
        // animation that replaced it. Clear immediately: no queued frozen entry.
        if (_crouchOwned && GodotObject.IsInstanceValid(_body)
            && _crouchState.GetCurrentAnimationName(CrouchTrack) == CrouchAnimation)
            _crouchState.ClearTrack(CrouchTrack);
        _crouchOwned = false;
        // Clear our track, not the shared state. Dispose would invalidate the
        // cached Godot.RefCounted wrapper still held by the idle controller.
        _crouchState = null;
    }

    private sealed class CrouchAnimationState(Variant native) : MegaAnimationState(native)
    {
        protected override IEnumerable<string> SpineMethods => [..base.SpineMethods, "clear_track"];
        public void ClearTrack(int track) { using var result = Call("clear_track", track); }
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

    private void OnAnimationStarted(GodotObject _, GodotObject __, GodotObject entry)
    {
        if (!_changingAnimation)
        {
            if (entry.Call("get_track_index").AsInt32() == CrouchTrack)
                _crouchOwned = false;
            Stop();
        }
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
        SetChargePose(0f);
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
        SetChargePose(0f);
        SetWorldOffset(Vector2.Zero);
    }
}
