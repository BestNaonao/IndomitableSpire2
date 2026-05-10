using BaseLib.Abstracts;
using BaseLib.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

public abstract class IndomitableTemporaryPower<T> : CustomTemporaryPowerModel where T : PowerModel
{
    // 处理贴图
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();
    public override string CustomPackedIconPath => $"packed/{Id.Entry.RemovePrefix().ToLowerInvariant()}_packed.tres".PowerImagePath();
    
    // 判断是正面Buff还是负面Buff
    protected virtual bool IsPositive => true;
    public override PowerType Type => IsPositive ? PowerType.Buff : PowerType.Debuff;
    
    // 底层实际操作的能力类型：T 泛型能力
    public override PowerModel InternallyAppliedPower => ModelDb.Power<T>();
    
    // 保留原版力量的悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<T>()];
    
    // 核心逻辑：定义如何施加这个能力
    protected override Func<Creature, decimal, Creature?, CardModel?, bool, Task> ApplyPowerFunc => 
        (target, amount, applier, source, silent) => 
            // BaseLib 会在回合结束时自动传入 (-(-amount)) 来返还内部能力，极其优雅！
            PowerCmd.Apply<T>(target, IsPositive ? amount : -amount, applier, source, silent);
}