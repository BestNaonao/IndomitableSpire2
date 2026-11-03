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

public sealed class WyvernFighter() : VersatileCardOption(OptionId)
{
    public const string OptionId = "fighter";
    
    public override IEnumerable<DynamicVar> CreateVars() => 
    [
        new DamageVar("FighterDamage", 13M, ValueProp.Move), 
        new CustomPowerVar<InterceptedPower>("FighterIntercepted", 3M)
    ];
    
    public override void Upgrade(CardModel source)
    {
        source.DynamicVars["FighterDamage"].UpgradeValueBy(4M);
        source.DynamicVars["FighterIntercepted"].UpgradeValueBy(2M);
    }
    
    public override async Task Execute(CardModel source, PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        var multiplier = play.IsAutoPlay ? 2M : 1M;
        var attack = await DamageCmd.Attack(source.DynamicVars["FighterDamage"].BaseValue * multiplier)
            .FromCard(source, play)
            .Targeting(play.Target)
            .ExecuteWithAircraftFlight(context);
        ((VersatileAircraftCard)source).RecordLoadoutDamageResults(attack.Results);
        if (play.Target.IsAlive) await PowerCmd.Apply<InterceptedPower>(context, play.Target, 
            source.DynamicVars["FighterIntercepted"].BaseValue * multiplier, source.Owner.Creature, source);
    }
}