using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class WarpDrivePower : TashkentPower
{
    private NWarpDriveVfx? _vfx;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomBigIconPath =>
        "res://TashkentSpire2/images/powers/big/WarpDrivePower.png";
    public override string CustomPackedIconPath =>
        "res://TashkentSpire2/images/powers/packed/WarpDrivePower.png";

    private NWarpDriveVfx? Vfx
    {
        get => _vfx != null && _vfx.IsValid() ? _vfx : null;
        set
        {
            AssertMutable();
            _vfx = value;
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Vfx = NWarpDriveVfx.Create(Owner);
        bool canApplyThisTurn = Owner.GetPower<DistancePower>()?.CanApplyWarpDriveThisTurn ?? true;
        Vfx?.SetSpeedActive(canApplyThisTurn);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        Vfx?.Dismiss();
        return Task.CompletedTask;
    }

    internal void SetSpeedVfxActive(bool isActive)
    {
        Vfx?.SetSpeedActive(isActive);
    }

    public override decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (amount < 0 && power is DistancePower && target == Owner)
        {
            return -1m;
        }
        return 1m;
    }
}
