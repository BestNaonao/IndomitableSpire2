using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using IndomitableSpire2.IndomitableSpire2Code.Rewards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

public sealed class EscortCommission() : CommissionCard(TargetType.Self)
{
    protected override int InitialMaxProgressAmount => 30;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [..base.CanonicalVars, new GoldVar(20)];
    
    // 监听格挡获取
    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature == Owner.Creature) AddProgress((int)amount);
        return Task.CompletedTask;
    }
    
    protected override Task GrantReward(PlayerChoiceContext choiceContext, Player player)
    {
        // 使用“国际订单”能力代替，将金币发放到战斗结束的奖励界面
        PowerCmd.Apply<InternationalOrdersPower>(choiceContext,
            player.Creature, DynamicVars.Gold.BaseValue, Owner.Creature, this);
        return Task.CompletedTask;
    }
    
    protected override void OnUpgrade() => DynamicVars.Gold.UpgradeValueBy(5);
    
    protected override IEnumerable<AllyRewardPreview> GetCommissionRewardPreviews(
        Player recipient, AllyRewardPreviewContext context) =>
        [new(recipient, AllyRewardPreviewIcons.Gold, DynamicVars.Gold.BaseValue)];
}