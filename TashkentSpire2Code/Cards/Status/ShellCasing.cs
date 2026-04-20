using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Status;

[Pool(typeof(StatusCardPool))]
public sealed class ShellCasing() : TashkentCard(1, CardType.Status, CardRarity.Status, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new LoadDynamicVar(1M)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
        
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
    }
}