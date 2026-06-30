using BaseLib.Hooks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

// 继承 IndomitablePower，并实现 BaseLib 的手牌上限修改接口
public sealed class SecondHangarPower : IndomitablePower, IMaxHandSizeModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; 
    
    // 实现接口：修改手牌上限
    public int ModifyMaxHandSize(Player player, int currentMaxHandSize)
    {
        // 确保这个钩子只对拥有该能力的玩家生效。直接加到当前上限上 Amount
        return player != Owner.Player ? currentMaxHandSize : currentMaxHandSize + Amount;
    }
}