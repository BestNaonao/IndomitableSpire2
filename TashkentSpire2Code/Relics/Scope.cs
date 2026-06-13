using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class Scope : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/Scope.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/Scope.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/Scope.png";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MarkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new MarkDynamicVar(1M)
    ];

    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (!(power is MarkPower))
        {
            return 0m;
        }
        if (giver != base.Owner.Creature || target == base.Owner.Creature)
        {
            return 0m;
        }
        return base.DynamicVars["TashkentSpire2-Mark"].BaseValue;
    }

    public override Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        if(power is MarkPower)
            Flash();
        return Task.CompletedTask;
    }
}