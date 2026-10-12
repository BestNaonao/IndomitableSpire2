using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;
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
        var fighter = (SeaHornetFighter)source;
        if (fighter.CombatState == null) return;
        var attack = await DamageCmd.Attack(fighter.DynamicVars["BombDamage"].BaseValue)
            .WithHitCount(fighter.DynamicVars["BombRepeat"].IntValue)
            .FromCard(fighter, cardPlay)
            .TargetingAllOpponents(fighter.CombatState)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(choiceContext);
        
        await PowerCmd.Apply<OnFirePower>(choiceContext, fighter.CombatState.HittableEnemies,
            fighter.DynamicVars["BombOnFire"].BaseValue, fighter.Owner.Creature, fighter);
        fighter.RecordLoadoutDamageResults(attack.Results);
    }
}