using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Options;

public sealed class SeaHornetBombs() : VersatileCardOption(OptionId)
{
    public const string OptionId = "bombs";
    
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