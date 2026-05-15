using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using TashkentSpire2.TashkentSpire2Code.RestSite;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class LoadingDevice : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/LoadingDevice.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/LoadingDevice.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/LoadingDevice.png";
    
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != base.Owner)
        {
            return false;
        }
        options.Add(new LoadRestSiteOption(player));
        return true;
    }
    
    public override bool ShouldDisableRemainingRestSiteOptions(Player player)
    {
        if (player != base.Owner) return true;

        var restUI = NRestSiteRoom.Instance;
        if (restUI == null) return true;

        int? lastIdx = RunManager.Instance.RestSiteSynchronizer.GetHoveredOptionIndex(player.NetId);

        if (player.RunState.CurrentMapPointHistoryEntry != null)
        {
            var history = player.RunState.CurrentMapPointHistoryEntry.GetEntry(player.NetId);
            if (history.RestSiteChoices.Count > 0)
            {
                string lastChoiceId = history.RestSiteChoices.Last();
                if (lastChoiceId == "TASHKENTSPIRE2-LOAD")
                {
                    Flash();
                    return false;
                }
            }
        }

        return true;
    }
}