using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class InterceptedPower : IndomitableTemporaryPower
{
    // 底层实际操作的能力类型：力量
    public override PowerModel InternallyAppliedPower => ModelDb.Power<StrengthPower>();
    
    // 如果没有特定绑定的单张卡牌，返回 null 即可（文本已由 CustomPowerModel 自动处理）
    public override AbstractModel OriginModel => null!;
    
    // 保留原版力量的悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];
    
    // 核心逻辑：定义如何施加这个能力
    protected override Func<Creature, decimal, Creature?, CardModel?, bool, Task> ApplyPowerFunc =>
        (target, amount, applier, source, silent) => 
            // 注意：截击是“失去”力量，所以我们传入 -amount
            // BaseLib 会在回合结束时自动传入 (-(-amount)) 来返还力量，极其优雅！
            PowerCmd.Apply<StrengthPower>(target, -amount, applier, source, silent);
}