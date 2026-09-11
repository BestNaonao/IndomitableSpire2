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
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Preparation>(base.IsUpgraded)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        List<CardModel> preparations = [];
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            Preparation preparation = base.CombatState.CreateCard<Preparation>(base.Owner);
            if (base.IsUpgraded)
            {
                CardCmd.Upgrade(preparation);
            }
            preparations.Add(preparation);
        }
        await CardPileCmd.AddGeneratedCardsToCombat(preparations, PileType.Hand, base.Owner);
    }
}
