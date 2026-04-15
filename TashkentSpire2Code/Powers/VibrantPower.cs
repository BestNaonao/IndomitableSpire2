using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public class VibrantPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/mark_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/mark_power.png";
    
    public override async Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        if (power.Owner == base.Owner)
        {
            await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
        }
    }
}