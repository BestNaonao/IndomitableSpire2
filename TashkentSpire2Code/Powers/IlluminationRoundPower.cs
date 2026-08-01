using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class IlluminationRoundPower : TashkentPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/IlluminationRoundPower.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/IlluminationRoundPower.png";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MarkPower>()];

    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (!(power is MarkPower))
        {
            return 0m;
        }
        if (target != base.Owner)
        {
            return 0m;
        }
        return this.Amount;
    }
}