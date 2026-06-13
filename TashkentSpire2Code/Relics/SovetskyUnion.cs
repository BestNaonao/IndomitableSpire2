using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class SovetskyUnion : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/SovetskyUnion.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/SovetskyUnion.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/SovetskyUnion.png";
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.ForEnergy(this)
    ];
    
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
        {
            return amount;
        }
        return amount - (decimal)base.DynamicVars.Energy.IntValue;
    }
    
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner)
        {
            return;
        }
        await PlayerCmd.GainEnergy(1m, base.Owner);
    }
    
    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler == base.Owner)
        {
            Flash();
            var Cards = Owner.PlayerCombatState?.AllPiles
                .SelectMany(p => p.Cards)
                .ToList();

            if (Cards != null)
            {
                foreach (var item in Cards)
                {
                    if (!item.EnergyCost.CostsX && item.EnergyCost.GetWithModifiers(CostModifiers.None) >= 0)
                    {
                        item.EnergyCost.AddThisCombat(1);
                    }
                }
            }
        }
    }
    
    public override bool ShouldPlayerResetEnergy(Player player)
    {
        if (player != base.Owner)
        {
            return true;
        }
        return false;
    }
}