using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;
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

public sealed class WyvernTorpedoes() : VersatileCardOption(OptionId)
{
    public const string OptionId = "torpedoes";
    
    public override IEnumerable<DynamicVar> CreateVars() =>
    [
        new DamageVar("TorpedoDamage", 10M, ValueProp.Move | ValueProp.Unblockable),
        new RepeatVar("TorpedoRepeat", 2),
        new CustomPowerVar<FloodingPower>("TorpedoFlooding", 3M),
        new CustomPowerVar<WyvernAloftPower>(1M)
    ];
    
    public override void Upgrade(CardModel source)
    {
        source.DynamicVars["TorpedoDamage"].UpgradeValueBy(3M);
        source.DynamicVars["TorpedoFlooding"].UpgradeValueBy(1M);
    }
    
    public override async Task Execute(CardModel source, PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        var attack = await DamageCmd.Attack(source.DynamicVars["TorpedoDamage"].BaseValue)
            .WithHitCount(source.DynamicVars["TorpedoRepeat"].IntValue)
            .FromCard(source, play)
            .Targeting(play.Target)
            .WithUnblockableDamage()
            .ExecuteWithAircraftFlight(context);
        ((VersatileAircraftCard)source).RecordLoadoutDamageResults(attack.Results);
        if (play.Target.IsAlive) await PowerCmd.Apply<FloodingPower>(context, play.Target,
                source.DynamicVars["TorpedoFlooding"].BaseValue, source.Owner.Creature, source);
        // 通过给予飞龙在天能力与战斗机形态联动，ApplyFor 中的 source 包含上下文语义。
        await WyvernAloftPower.ApplyFor(context, (WyvernS4)source);
    }
}