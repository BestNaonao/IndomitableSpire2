using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Tags;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class FullSalvoPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/mark_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/mark_power.png";
    
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        
        if (card.Owner.Creature != base.Owner)
        {
            return false;
        }
        
        if (!card.Tags.Contains(TashkentTags.Ammunition))
        {
            return false;
        }
        
        bool isValidPile = card.Pile?.Type switch
        {
            PileType.Hand => true,
            PileType.Play => true,
            _ => false
        };

        if (!isValidPile)
        {
            return false;
        }

        modifiedCost = 0m; 
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == base.Owner && 
            cardPlay.Card.Tags.Contains(TashkentTags.Ammunition))
        {
            bool isValidPile = cardPlay.Card.Pile?.Type switch
            {
                PileType.Hand => true,
                PileType.Play => true,
                _ => false
            };

            if (isValidPile)
            {
                await PowerCmd.Decrement(this);
            }
        }
    }
}