using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class InterceptedPower : IndomitablePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 关联原版力量的悬浮提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];
    
    // 1. 首次施加时：扣除目标力量
    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 施加负数力量，silent: true 可以阻止原版的跳字动画
        await PowerCmd.Apply<StrengthPower>(target, -amount, applier, cardSource, silent: true);
    }
    
    // 2. 层数叠加时（如连续打出海喷火）：追加扣除力量
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 原版防重复判定：如果变化量等于当前总层数，说明是首次施加，直接跳过，否则追加扣除这部分变化量的力量
        if (power != this || amount == Amount) return;
        await PowerCmd.Apply<StrengthPower>(Owner, -amount, applier, cardSource, silent: true);
    }
    
    // 3. 敌人回合结束时：归还力量并移除自身
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        Flash();
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<StrengthPower>(Owner, Amount, Owner, null, silent: true);
    }
}