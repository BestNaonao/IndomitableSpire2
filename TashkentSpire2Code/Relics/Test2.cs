using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class Test2 : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/Thruster.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/Thruster.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/Thruster.png";
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(2)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.ForEnergy(this)
    ];
    
    
}