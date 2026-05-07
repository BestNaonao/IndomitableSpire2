using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class DynamicVarExtensions
{
    /// <summary>
    /// 为继承自 PowerVar 的动态变量自动绑定对应能力的悬浮提示框。
    /// 彻底消除手动重写 ExtraHoverTips 的样板代码！
    /// </summary>
    public static TDynamicVar WithPowerTooltip<TDynamicVar, TPower>(this TDynamicVar var) 
        where TDynamicVar : PowerVar<TPower> 
        where TPower : PowerModel
    {
        // 利用 BaseLib 的底层字段，直接绑定原生的能力 Tip 生成逻辑
        BaseLib.Extensions.DynamicVarExtensions.DynamicVarTips[var] = HoverTipFactory.FromPower<TPower>;
        return var;
    }
}