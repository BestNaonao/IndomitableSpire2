using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class YogaPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 核心1：在数值真正发生改变之前拦截干劲数值的增减意图（完美覆盖“获得”与“消耗”）
    public override async Task BeforePowerAmountChanged(
        PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        // 确保改变的是干劲，目标是自己，且确实有增减意图（amount != 0）
        if (power is MotivationPower && target == Owner && amount != 0) 
            await TriggerBonus(new ThrowingPlayerChoiceContext());
    }
    
    // 核心2：拦截卡牌打出行为（完美覆盖“需求”）
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 如果打出的牌属于自己，且这张牌上有“干劲需求”
        if (cardPlay.Card.Owner == Owner.Player && cardPlay.Card.RequiresMotivation()) 
            await TriggerBonus(choiceContext);
    }
    
    // 获得等量于层数的护盾（不受敏捷影响，与原版激怒逻辑一致）
    private async Task TriggerBonus(PlayerChoiceContext choiceContext)
    {
        Flash();
        await CustomCreatureCmd.GainShield(
            choiceContext, Owner, Amount, ValueProp.Move, null, Owner);
    }
    
    // 在己方回合结束时自动移除
    public override async Task AfterSideTurnEndLate(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}