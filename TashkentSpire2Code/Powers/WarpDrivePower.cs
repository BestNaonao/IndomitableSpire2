using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class WarpDrivePower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/WarpDrivePower.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/WarpDrivePower.png";

    public override decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (amount < 0 && power is DistancePower && target == this.Owner)
        {
            return -1m;
        }
        return 1m;
    }
}