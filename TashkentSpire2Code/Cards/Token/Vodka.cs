using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Token;

public class Vodka() : TashkentCard(0, CardType.Status, CardRarity.Token, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VigorPower>(3M),
        new CardsVar(1)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VigorPower>(base.Owner.Creature, base.DynamicVars["VigorPower"].IntValue, base.Owner.Creature, this);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars["CardsVar"].IntValue, base.Owner);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["VigorPower"].UpgradeValueBy(3M);
    }
}