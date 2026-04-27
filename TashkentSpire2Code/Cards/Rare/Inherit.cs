using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class Inherit() : TashkentCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2),
        new EnergyVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.IntValue, base.Owner);
        
        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            this.Owner,
            new CardSelectorPrefs(new LocString("cards", this.Id.Entry + ".selectionScreenPrompt"), 0, 999),
            null,
            this)).ToList();

        if (selected.Count == 0)
            return;
        
        foreach (CardModel item in selected)
        {
            await CardCmd.Exhaust(choiceContext, item);
        }
        
        int amount = selected.Count;
        await PlayerCmd.GainEnergy(amount, base.Owner);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(2M);
    }
}