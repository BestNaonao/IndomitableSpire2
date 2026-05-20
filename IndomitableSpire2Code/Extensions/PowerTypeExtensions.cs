using MegaCrit.Sts2.Core.Entities.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class PowerTypeExtensions
{
    /// <summary>
    /// 反转能力的极性：Buff 变 Debuff，Debuff 变 Buff，None 保持不变。
    /// </summary>
    public static PowerType Invert(this PowerType type) => type switch
    {
        PowerType.Buff => PowerType.Debuff,
        PowerType.Debuff => PowerType.Buff,
        _ => type
    };
    
    /// <summary>
    /// 根据条件决定是否反转能力的极性。
    /// </summary>
    public static PowerType InvertIf(this PowerType type, bool condition) => 
        condition ? type.Invert() : type;
}