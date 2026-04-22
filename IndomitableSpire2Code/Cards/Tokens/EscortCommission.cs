using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Tokens;

public sealed class EscortCommission() : CommissionCard(TargetType.Self)
{
    protected override int InitialMaxProgressAmount => 30;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        ..base.CanonicalVars,
        new GoldVar(20)
    ];
    
    // 监听格挡获取
    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature == Owner.Creature)
            AddProgress((int)amount);
        return Task.CompletedTask;
    }
    
    protected override Task GrantReward(PlayerChoiceContext choiceContext, Player player)
    {
        // 效防原版“国王资产”，将金币发放到战斗结束的奖励界面
        if (Owner.RunState.CurrentRoom is CombatRoom room)
            room.AddExtraReward(player, new GoldReward(DynamicVars.Gold.IntValue, player));
        return Task.CompletedTask;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Gold.UpgradeValueBy(5);
    }
}