using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class RestingEyesPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    // Amount 只表示剩余回合数；养神的抽牌加成始终为 1。
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 暂用休憩的艺术图标，直到闭目有独立美术资源。
    public override string CustomBigIconPath => "res://IndomitableSpire2/images/powers/big/art_of_resting_power.png";
    public override string CustomPackedIconPath => "res://IndomitableSpire2/images/powers/packed/art_of_resting_power_packed.tres";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Refresh>()];
    
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        foreach (var card in Owner.Player?.PlayerCombatState?.AllCards.OfType<Refresh>() ?? [])
            card.SyncRestingEyesBonus();
        return Task.CompletedTask;
    }
    
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        // 移出战斗的牌已不再接收自身钩子，由仍在场的能力负责撤销。
        // 按卡牌当前拥有者重算，也处理玩家间转移；普通换牌堆不会重复加成。
        if (card is Refresh refresh) refresh.SyncRestingEyesBonus();
        return Task.CompletedTask;
    }
    
    public override Task AfterRemoved(Creature oldOwner)
    {
        foreach (var card in oldOwner.Player?.PlayerCombatState?.AllCards.OfType<Refresh>() ?? [])
            card.SyncRestingEyesBonus();
        return Task.CompletedTask;
    }
    
    public override async Task AfterSideTurnEndLate(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // 保留到本回合的普通回合结束效果结算完毕；其他玩家的额外回合不消耗时长。
        if (side == Owner.Side && participants.Contains(Owner))
            await PowerCmd.Decrement(this);
    }
}