using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

public sealed class ExerciseCommission() : CommissionCard(TargetType.Self)
{
    protected override int InitialMaxProgressAmount => 50;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ..base.CanonicalVars,
        new("PowerAmount", 2m)
    ];
    
    // 监听造成的伤害
    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer == Owner.Creature && target.IsEnemy)
            AddProgress(result.TotalDamage); // 记录总伤害
        return Task.CompletedTask;
    }
    
    protected override async Task GrantReward(PlayerChoiceContext choiceContext, Player player)
    {
        await PowerCmd.Apply<StrengthPower>(player.Creature, DynamicVars["PowerAmount"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(player.Creature, DynamicVars["PowerAmount"].BaseValue, Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["PowerAmount"].UpgradeValueBy(1);
    }
}