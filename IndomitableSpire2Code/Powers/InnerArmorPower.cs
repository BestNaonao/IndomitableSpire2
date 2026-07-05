using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

// 继承监听接口 IAfterShieldBrokenSubscriber
public sealed class InnerArmorPower : IndomitablePower, IAfterShieldBrokenSubscriber
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 监听护盾破碎事件
    public async Task AfterShieldBroken(Creature target)
    {
        // 确保是自己身上的护盾破了
        if (target != Owner) return;
        
        Flash();
        
        // 补偿护盾（因为这里没有直接的 PlayerChoiceContext，使用引擎通用的 ThrowingContext）
        await CustomCreatureCmd.GainShield(
            new ThrowingPlayerChoiceContext(), Owner, Amount, ValueProp.Move, null, Owner);
        
        // 触发完毕后，功成身退，自我移除
        await PowerCmd.Remove(this);
    }
}