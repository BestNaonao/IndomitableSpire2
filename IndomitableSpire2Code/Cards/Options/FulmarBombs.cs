using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Options;

public sealed class FulmarBombs() : VersatileCardOption(OptionId)
{
    public const string OptionId = "bombs";
    
    public override IEnumerable<DynamicVar> CreateVars() =>
    [
        new DamageVar("BombDamage", 3M, ValueProp.Move),
        new RepeatVar("BombRepeat", 2),
        new CustomPowerVar<FulmarEscortPower>(4M)
    ];
    
    public override void Upgrade(CardModel source)
    {
        source.DynamicVars["BombDamage"].UpgradeValueBy(1M);
        source.DynamicVars["FulmarEscortPower"].UpgradeValueBy(2M);
    }
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        
        var attack = await DamageCmd.Attack(source.DynamicVars["BombDamage"].BaseValue)
            .WithHitCount(source.DynamicVars["BombRepeat"].IntValue)
            .FromCard(source, cardPlay)
            .Targeting(cardPlay.Target)
            .ExecuteWithAircraftFlight(choiceContext);
        ((VersatileAircraftCard)source).RecordLoadoutDamageResults(attack.Results);
        
        await PowerCmd.Apply<FulmarEscortPower>(choiceContext, source.Owner.Creature,
            source.DynamicVars["FulmarEscortPower"].BaseValue, source.Owner.Creature, source);
    }
}