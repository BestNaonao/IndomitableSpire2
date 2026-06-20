using BaseLib.Abstracts;
using BaseLib.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

public abstract class IndomitableTemporaryPower<T> : CustomTemporaryPowerModel where T : PowerModel
{
    // 处理贴图
    public override string CustomBigIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();
    public override string CustomPackedIconPath => $"packed/{Id.Entry.RemovePrefix().ToLowerInvariant()}_packed.tres".PowerImagePath();
    
    // 【核心改动 1】：利用扩展方法，根据 InvertInternalPowerAmount 动态反转内部能力的极性
    public override PowerType Type => InternallyAppliedPower.Type.InvertIf(InvertInternalPowerAmount);
    
    // 底层实际操作的能力类型：T 泛型能力
    public override PowerModel InternallyAppliedPower => ModelDb.Power<T>();
    
    // 内部能力的悬浮提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<T>()];
    
    // 【核心改动 2】定义如何施加这个能力：签名加入了 PlayerChoiceContext。
    // 因为 BaseLib 已经通过 InvertInternalPowerAmount 在底层帮我们把 amount 的正负号处理好了，
    // 这里我们直接原样传递 amount，代码变得极其干净！
    protected override Func<PlayerChoiceContext, Creature, decimal, Creature?, CardModel?, bool, Task> ApplyPowerFunc => 
        (_, target, amount, applier, source, silent) => 
            PowerCmd.Apply<T>(new ThrowingPlayerChoiceContext(), target, amount, applier, source, silent);
}