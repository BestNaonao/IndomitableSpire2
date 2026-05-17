using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;

/// <summary>
/// 用于给予护盾的动态变量。
/// </summary>
public class ShieldVar : BlockVar
{
    // 设定默认标识符，方便在 json 文件中精准匹配
    public const string DefaultName = "Shield";
    
    // 默认构造函数，直接传入数值即可，已经自带提示栏
    public ShieldVar(decimal baseValue, ValueProp props) : base(DefaultName, baseValue, props)
    {
        // this.WithTooltip();
    }
    
    // 重载构造函数，保留原版高度的灵活性
    public ShieldVar(string name, decimal baseValue, ValueProp props) : base(name, baseValue, props)
    {
        // this.WithTooltip();
    }
}