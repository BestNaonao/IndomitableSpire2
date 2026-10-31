using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Rewards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

public sealed class ExerciseCommission() : CommissionCard(TargetType.Self)
{
    protected override int InitialMaxProgressAmount => 40;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ..base.CanonicalVars,
        new PowerVar<StrengthPower>(2m),
        new PowerVar<DexterityPower>(2m)
    ];
    
    // 监听并记录造成的总伤害
    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, 
        Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer == Owner.Creature && target.IsEnemy) AddProgress(result.TotalDamage + result.OverkillDamage);
        return Task.CompletedTask;
    }
    
    protected override async Task GrantReward(PlayerChoiceContext choiceContext, Player player)
    {
        await PowerCmd.Apply<StrengthPower>(choiceContext: choiceContext, 
            player.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(choiceContext: choiceContext, 
            player.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1m);
        DynamicVars.Dexterity.UpgradeValueBy(1m);
    }
    
    protected override IEnumerable<AllyRewardPreview> GetCommissionRewardPreviews(
        Player recipient, AllyRewardPreviewContext context) =>
    [
        new(recipient, AllyRewardPreviewIcons.Strength, DynamicVars.Strength.BaseValue),
        new(recipient, AllyRewardPreviewIcons.Dexterity, DynamicVars.Dexterity.BaseValue)
    ];
}