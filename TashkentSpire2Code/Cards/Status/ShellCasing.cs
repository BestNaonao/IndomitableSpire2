using BaseLib.Utils;
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
    
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
        
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card == this)
        {
            CardPile handPile = PileType.Hand.GetPile(base.Owner);
            if (handPile == null || handPile.Cards.Count == 0) return;

            var ammunitionCards = handPile.Cards
                .Where(c => c is IAmmunitionCard)
                .ToList();

            if (ammunitionCards.Count == 0) return;

            var priorityCandidates = ammunitionCards
                .Where(c => c is IAmmunitionCard ammuCard && ammuCard.CurrentAmmu < ammuCard.MaxAmmu)
                .ToList();

            var finalCandidates = priorityCandidates.Count > 0 ? priorityCandidates : ammunitionCards;

            CardModel? loadcard = base.Owner.RunState.Rng.CombatCardSelection.NextItem(finalCandidates);

            if (loadcard != null)
            {
                await Loadcmd.Execute(choiceContext, loadcard, DynamicVars["TashkentSpire2-Load"].IntValue);
            }
        }
    }

    public override async Task AfterCardDrawnEarly(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card == this)
        {
            CardPile handPile = PileType.Hand.GetPile(base.Owner);
            if (handPile == null || handPile.Cards.Count == 0) return;

            var ammunitionCards = handPile.Cards
                .Where(c => c is IAmmunitionCard)
                .ToList();

            if (ammunitionCards.Count == 0) return;

            var priorityCandidates = ammunitionCards
                .Where(c => c is IAmmunitionCard ammuCard && ammuCard.CurrentAmmu < ammuCard.MaxAmmu)
                .ToList();

            var finalCandidates = priorityCandidates.Count > 0 ? priorityCandidates : ammunitionCards;

            CardModel? loadcard = base.Owner.RunState.Rng.CombatCardSelection.NextItem(finalCandidates);

            if (loadcard != null)
            {
                await Loadcmd.Execute(choiceContext, loadcard, DynamicVars["TashkentSpire2-Load"].IntValue);
            }
        }
    }
}