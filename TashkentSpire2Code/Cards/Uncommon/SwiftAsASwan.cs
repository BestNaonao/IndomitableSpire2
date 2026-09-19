using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class SwiftAsASwan() : TashkentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Retain)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1),
        new RetreatDynamicVar(3M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DistancePower>(choiceContext, base.Owner.Creature, -base.DynamicVars["TashkentSpire2-Retreat"].BaseValue, base.Owner.Creature, this);
        int num = Math.Min(base.DynamicVars.Cards.IntValue, CardPile.MaxCardsInHand - PileType.Hand.GetPile(base.Owner).Cards.Count);
        if (num > 0)
        {
            var selectedCards = (await CardSelectCmd.FromCombatPile(
                choiceContext, 
                PileType.Discard.GetPile(base.Owner), 
                base.Owner, 
                new CardSelectorPrefs(base.SelectionScreenPrompt, 0, num)
            )).ToList();
            
            if (selectedCards != null && selectedCards.Count > 0)
            {
                foreach (var card in selectedCards)
                {
                    card.AddKeyword(CardKeyword.Retain); 
                }
                await CardPileCmd.Add(selectedCards, PileType.Hand);
            }
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Retreat"].UpgradeValueBy(1M);
        DynamicVars.Cards.UpgradeValueBy(1M);
    }
}
