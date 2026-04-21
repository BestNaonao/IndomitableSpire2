using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class BackInAction() : TashkentCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        CardPile discardPile = PileType.Discard.GetPile(base.Owner);
        
        var cardsToPlay = discardPile.Cards
            .Where(c => !c.Keywords.Contains(CardKeyword.Unplayable)) 
            .ToList()
            .StableShuffle(base.Owner.RunState.Rng.Shuffle)
            .Take(2)
            .ToList();
        
        foreach (var card in cardsToPlay)
        {
            if (CombatManager.Instance.IsOverOrEnding)
            {
                break;
            }

            Creature target = null;
            if (card.TargetType == TargetType.AnyEnemy)
            {
                target = base.Owner.RunState.Rng.CombatTargets.NextItem(base.CombatState.HittableEnemies)!;
            }
            
            await CardCmd.AutoPlay(choiceContext, card, target);
        }
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}