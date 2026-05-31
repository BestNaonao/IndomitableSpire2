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
        BaseLib.Extensions.DynamicVarExtensions.DynamicVarTips[var] = _ => HoverTipFactory.FromPower<TPower>();
        return var;
    }
    
    /// <summary>
    /// 重载 2：服务于任意普通的 DynamicVar（如 ShieldVar、DamageVar 等）。
    /// 只需要填入一个泛型。
    /// 巧妙之处：返回值是基类 DynamicVar，这不仅免去了输入第二个泛型的麻烦，
    /// 还在隐式类型数组 [...] 初始化时完美通过了编译！
    /// </summary>
    public static DynamicVar WithPowerTooltip<TPower>(this DynamicVar var) 
        where TPower : PowerModel
    {
        BaseLib.Extensions.DynamicVarExtensions.DynamicVarTips[var] = _ => HoverTipFactory.FromPower<TPower>();
        return var;
    }
}