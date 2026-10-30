using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Options;

public sealed class FulmarFighter() : VersatileCardOption(OptionId)
{
    public const string OptionId = "fighter";
    
    public override IEnumerable<DynamicVar> CreateVars() => 
    [
        new DamageVar("FighterDamage", 2M, ValueProp.Move), 
        new RepeatVar("FighterRepeat", 3), 
        new("FighterRecon", 2M)
    ];
    
    public override void Upgrade(CardModel source)
    {
        source.DynamicVars["FighterDamage"].UpgradeValueBy(1M);
        source.DynamicVars["FighterRecon"].UpgradeValueBy(1M);
    }
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attack = await DamageCmd.Attack(source.DynamicVars["FighterDamage"].BaseValue)
            .WithHitCount(source.DynamicVars["FighterRepeat"].IntValue)
            .FromCard(source, cardPlay)
            .TargetingRandomOpponents(source.CombatState!)
            .ExecuteWithAircraftFlight(choiceContext);
        ((VersatileAircraftCard)source).RecordLoadoutDamageResults(attack.Results);
        // 包括完全被格挡及被击杀的敌人；同一敌人被多段命中只计一次，整组攻击只侦察一次。
        var targets = attack.Results.SelectMany(hit => hit)
            .Select(result => result.Receiver)
            .Where(creature => creature.IsEnemy)
            .Distinct().Count();
        await ReconCommand.Recon(source.Owner, targets * source.DynamicVars["FighterRecon"].IntValue).Execute(choiceContext);
    }
}