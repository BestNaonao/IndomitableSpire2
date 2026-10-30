using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Options;

public sealed class SeaFuryFighter() : VersatileCardOption(OptionId)
{
    public const string OptionId = "fighter";
    
    public override IEnumerable<DynamicVar> CreateVars() => 
        [new DamageVar("FighterDamage", 8M, ValueProp.Move)];
    
    public override void Upgrade(CardModel source) => source.DynamicVars["FighterDamage"].UpgradeValueBy(2M);
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var attack = await DamageCmd.Attack(source.DynamicVars["FighterDamage"].BaseValue)
            .FromCard(source, cardPlay)
            .Targeting(cardPlay.Target)
            .ExecuteWithAircraftFlight(choiceContext);
        ((VersatileAircraftCard)source).RecordLoadoutDamageResults(attack.Results);
    }
}