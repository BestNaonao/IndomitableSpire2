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

public sealed class FirebrandTorpedoes() : VersatileCardOption(OptionId)
{
    public const string OptionId = "torpedoes";
    
    public override IEnumerable<DynamicVar> CreateVars() => 
    [
        new DamageVar("TorpedoDamage", 8M, ValueProp.Move), 
        new CustomPowerVar<IlluminatedPower>("TorpedoIlluminated", 4M)
    ];
    
    public override void Upgrade(CardModel source)
    {
        source.DynamicVars["TorpedoDamage"].UpgradeValueBy(3M);
        source.DynamicVars["TorpedoIlluminated"].UpgradeValueBy(2M);
    }
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var aircraft = (FirebrandTF4)source;
        var attack = await DamageCmd.Attack(source.DynamicVars["TorpedoDamage"].BaseValue)
            .FromCard(source, cardPlay)
            .Targeting(cardPlay.Target)
            .ExecuteWithAircraftFlight(choiceContext);
        
        if (cardPlay.Target.IsAlive)
            await PowerCmd.Apply<IlluminatedPower>(choiceContext, cardPlay.Target,
                source.DynamicVars["TorpedoIlluminated"].BaseValue, source.Owner.Creature, source);
        
        aircraft.RecordLoadoutDamageResults(attack.Results);
    }
}