using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Options;

public sealed class FirebrandBombs() : VersatileCardOption(OptionId)
{
    public const string OptionId = "bombs";
    
    public override IEnumerable<DynamicVar> CreateVars() => 
    [
        new DamageVar("BombDamage", 8M, ValueProp.Move), 
        new RepeatVar("BombRepeat", 2)
    ];
    
    public override void Upgrade(CardModel source) => source.DynamicVars["BombDamage"].UpgradeValueBy(4M);
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var aircraft = (FirebrandTF4)source;
        var attack = await DamageCmd.Attack(source.DynamicVars["BombDamage"].BaseValue)
            .WithHitCount(source.DynamicVars["BombRepeat"].IntValue)
            .FromCard(source, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        
        aircraft.RecordLoadoutDamageResults(attack.Results);
    }
}