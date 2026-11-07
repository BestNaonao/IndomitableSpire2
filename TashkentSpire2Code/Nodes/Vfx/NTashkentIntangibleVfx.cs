using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

/// <summary>Source-independent Intangible opacity, applied only to the body, never its UI or forms.</summary>
public partial class NTashkentIntangibleVfx : Node
{
    private NCreature _owner = null!;
    private Node2D? _body;
    private float _factor = 1f;
    private float _target = 1f;
    private bool _stopped;

    public static void Attach(NCreature owner)
    {
        if (TestMode.IsOn || owner.Entity.Player?.Character is not TashkentCharacter
            || owner.GetChildren().OfType<NTashkentIntangibleVfx>().Any(n => !n.IsQueuedForDeletion()))
            return;
        owner.AddChildSafely(new NTashkentIntangibleVfx { Name = "TashkentIntangible", _owner = owner });
    }

    public override void _EnterTree()
    {
        _owner.Entity.PowerApplied += OnPower;
        _owner.Entity.PowerRemoved += OnPower;
        _owner.Entity.PowerIncreased += OnPowerIncreased;
        _owner.Entity.PowerDecreased += OnPowerDecreased;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
    }

    public override void _Ready()
    {
        _body = _owner.Body;
        Refresh();
        // Also handles saves/restored combats that already contain Intangible.
        ApplyFactor(_target);
        SetProcess(_target < 1f);
    }

    private void OnPower(PowerModel power) { if (power is IntangiblePower) Refresh(); }
    private void OnPowerIncreased(PowerModel power, int _, bool silent) => OnPower(power);
    private void OnPowerDecreased(PowerModel power, bool silent) => OnPower(power);
    private void Refresh()
    {
        if (_stopped)
            return;
        _target = _owner.Entity.GetPowerAmount<IntangiblePower>() > 0 ? 0.5f : 1f;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_stopped)
            return;
        if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding)
        {
            Stop();
            return;
        }
        if (_body != _owner.Body)
        {
            ApplyFactor(1f);
            _body = _owner.Body;
            ApplyFactor(_target);
        }
        ApplyFactor(Mathf.MoveToward(_factor, _target, Math.Max(0f, (float)delta) * 4f));
        if (_target == 1f && Mathf.IsEqualApprox(_factor, 1f))
            SetProcess(false);
    }

    private void ApplyFactor(float factor)
    {
        if (GodotObject.IsInstanceValid(_body))
        {
            Color color = _body!.Modulate;
            color.A = Mathf.Clamp(color.A / _factor * factor, 0f, 1f);
            _body.Modulate = color;
        }
        _factor = factor;
    }

    private void Stop()
    {
        _stopped = true;
        ApplyFactor(1f);
        SetProcess(false);
        this.QueueFreeSafely();
    }
    private void OnCombatEnded(CombatRoom _) => Stop();
    public override void _ExitTree()
    {
        _owner.Entity.PowerApplied -= OnPower;
        _owner.Entity.PowerRemoved -= OnPower;
        _owner.Entity.PowerIncreased -= OnPowerIncreased;
        _owner.Entity.PowerDecreased -= OnPowerDecreased;
        CombatManager.Instance.CombatEnded -= OnCombatEnded;
        ApplyFactor(1f);
    }
}
