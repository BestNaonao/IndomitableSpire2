using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class Test1 : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/Thruster.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/Thruster.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/Thruster.png";
    
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