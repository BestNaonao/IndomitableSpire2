using IndomitableSpire2.IndomitableSpire2Code.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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
    
    // 核心：在数值真正发生改变（或被截断）之前，精准拦截动作意图
    public override async Task BeforePowerAmountChanged(
        PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        // 确保改变的是干劲，目标是自己，且确实有增减意图（amount != 0）
        if (power is MotivationPower && target == Owner && amount != 0)
        {
            Flash();
            // 获得等量于层数的护盾（不受敏捷影响，与原版激怒逻辑一致）
            await CustomCreatureCmd.GainShield(
                new ThrowingPlayerChoiceContext(), Owner, Amount, ValueProp.Move, null, Owner);
        }
    }

    // 在己方回合结束时自动移除
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}