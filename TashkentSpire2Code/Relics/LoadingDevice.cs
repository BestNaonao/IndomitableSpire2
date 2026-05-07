using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class LoadingDevice : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/EjectionStart.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/EjectionStart.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/EjectionStart.png";
    
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != base.Owner)
        {
            return false;
        }
        //options.Add(new DigRestSiteOption(player));
        return true;
    }
}