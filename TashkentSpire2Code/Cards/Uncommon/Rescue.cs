using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class Rescue() : TashkentCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        int xValue = ResolveEnergyXValue();
        if (base.IsUpgraded)
        {
            xValue++;
        }
        
        var pile = PileType.Hand.GetPile(base.Owner);
        
        var candidates = pile.Cards
            .Where(card => 
                    card.Owner == base.Owner &&
                    card.CostsEnergyOrStars(includeGlobalModifiers: true)
            )
            .ToList();

        if (candidates.Count <= xValue)
        {
            foreach (var card in candidates)
            {
                card.SetToFreeThisTurn();
            }
        }
        else
        {
            for (int i = 0; i < xValue; i++)
            {
                var selected = base.Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
                if (selected != null)
                {
                    candidates.Remove(selected);
                    selected.SetToFreeThisTurn();
                }
            }
        }
    }
}