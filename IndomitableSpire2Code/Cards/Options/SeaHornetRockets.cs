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

public sealed class SeaHornetRockets() : VersatileCardOption(OptionId)
{
    public const string OptionId = "rockets";
    
    public override IEnumerable<DynamicVar> CreateVars() =>
    [
        new DamageVar("RocketDamage", 2M, ValueProp.Move),
        new RepeatVar("RocketRepeat", 8),
        new CustomPowerVar<OnFirePower>("RocketOnFire", 4M)
    ];
    
    public override void Upgrade(CardModel source)
    {
        source.DynamicVars["RocketDamage"].UpgradeValueBy(1M);
        source.DynamicVars["RocketOnFire"].UpgradeValueBy(1M);
    }
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var fighter = (SeaHornetFighter)source;
        if (fighter.CombatState == null) return;
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        var attack = await DamageCmd.Attack(fighter.DynamicVars["RocketDamage"].BaseValue)
            .WithHitCount(fighter.DynamicVars["RocketRepeat"].IntValue)
            .FromCard(fighter, cardPlay)
            .OnlyPlayAnimOnce()
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_fire_burst")
            .Execute(choiceContext);
        
        if (cardPlay.Target.IsAlive)
            await PowerCmd.Apply<OnFirePower>(choiceContext, cardPlay.Target,
                fighter.DynamicVars["RocketOnFire"].BaseValue, fighter.Owner.Creature, fighter);
        fighter.RecordLoadoutDamageResults(attack.Results);
    }
}