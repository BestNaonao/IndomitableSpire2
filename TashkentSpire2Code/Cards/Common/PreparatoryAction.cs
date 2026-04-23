using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards.Token;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public sealed class PreparatoryAction() : TashkentCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<ChargePreparation>(),
        HoverTipFactory.FromCard<RetreatPreparation>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        CardModel card1 = base.CombatState.CreateCard<ChargePreparation>(base.Owner);
        CardModel card2 = base.CombatState.CreateCard<RetreatPreparation>(base.Owner);
        if (this.IsUpgraded)
        {
            CardCmd.Upgrade(card1);
            CardCmd.Upgrade(card2);
        }

        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            await CardPileCmd.AddGeneratedCardToCombat(card1, PileType.Hand, addedByPlayer: true);
            await CardPileCmd.AddGeneratedCardToCombat(card2, PileType.Hand, addedByPlayer: true);
        }
    }
}