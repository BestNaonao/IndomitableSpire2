using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class VictoriousSong() : IndomitableCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<VictoriousSongPower>(3M), 
        ..MakeCalculatedVar("DamageReduction", 1, (card, _) => 
            card.DynamicVars["VictoriousSongPower"].PreviewValue - card.DynamicVars["VictoriousSongPower"].BaseValue)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        await PowerCmd.Apply<VictoriousSongPower>(
            choiceContext: choiceContext,
            target: Owner.Creature,
            amount: DynamicVars["VictoriousSongPower"].BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["VictoriousSongPower"].UpgradeValueBy(1M);
        DynamicVars["DamageReductionBase"].UpgradeValueBy(1M);
    }
}