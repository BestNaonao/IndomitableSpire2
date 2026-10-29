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

public sealed class SeaHornetBombs() : VersatileCardOption(OptionId)
{
    public const string OptionId = "bombs";
    
    public override IEnumerable<DynamicVar> CreateVars() =>
    [
        new DamageVar("BombDamage", 8M, ValueProp.Move),
        new RepeatVar("BombRepeat", 2),
        new CustomPowerVar<OnFirePower>("BombOnFire", 3M)
    ];
    
    public override void Upgrade(CardModel source)
    {
        source.DynamicVars["BombDamage"].UpgradeValueBy(3M);
        source.DynamicVars["BombOnFire"].UpgradeValueBy(1M);
    }
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (source.CombatState is not { } combatState) return;
        var attack = await DamageCmd.Attack(source.DynamicVars["BombDamage"].BaseValue)
            .WithHitCount(source.DynamicVars["BombRepeat"].IntValue)
            .FromCard(source, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_fire_burst")
            .ExecuteWithAircraftFlight(choiceContext);
        
        await PowerCmd.Apply<OnFirePower>(choiceContext, combatState.HittableEnemies,
            source.DynamicVars["BombOnFire"].BaseValue, source.Owner.Creature, source);
        ((VersatileAircraftCard)source).RecordLoadoutDamageResults(attack.Results);
    }
}