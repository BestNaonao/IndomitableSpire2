using MegaCrit.Sts2.Core.Entities.Relics;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class Thruster : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/Thruster.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/Thruster.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/Thruster.png";
}