using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using TashkentSpire2.TashkentSpire2Code.Commands;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Status;

[Pool(typeof(StatusCardPool))]
public sealed class ShellCasing() : TashkentCard(1, CardType.Status, CardRarity.Status, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new LoadDynamicVar(1M)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
        
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card == this)
        {
            CardSelectorPrefs cardSelectorPrefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 1);
        
            CardModel? loadcard = (await CardSelectCmd.FromHand(
                choiceContext, 
                base.Owner, 
                cardSelectorPrefs, 
                (CardModel c) => c is IAmmunitionCard, 
                this
            )).FirstOrDefault();

            if (loadcard != null)
            {
                await Loadcmd.Execute(choiceContext, loadcard, DynamicVars["TashkentSpire2-Load"].IntValue);
            }
        }
    }
}