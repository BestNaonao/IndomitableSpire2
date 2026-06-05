using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class Test2 : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    
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

    public override Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Creature.Side)
        {
            CardPile handPile = PileType.Hand.GetPile(base.Owner);

            if (handPile?.Cards == null || handPile.Cards.Count == 0)
            {
                return Task.CompletedTask;
            }

            int maxCost = handPile.Cards.Max(c => c.EnergyCost.GetWithModifiers(CostModifiers.None));

            List<CardModel> maxCostCards = handPile.Cards
                .Where(c => c.EnergyCost.GetWithModifiers(CostModifiers.None) == maxCost)
                .ToList();

            CardModel? cardModel = base.Owner.RunState.Rng.CombatCardSelection.NextItem(maxCostCards);
        
            if (cardModel != null && !cardModel.EnergyCost.CostsX)
            {
                int reduceAmount = base.DynamicVars.Energy.IntValue;
                cardModel.EnergyCost.AddThisTurnOrUntilPlayed(-reduceAmount);
                Flash();
            }
        }
        return Task.CompletedTask;
    }
}