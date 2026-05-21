using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;

/// <summary>
/// 用于显示伤害乘率的动态变量。
/// </summary>
public class DamageMultiplierVar : DynamicVar
{
    // 设定默认标识符，方便在 json 文件中精准匹配
    public const string DefaultName = "DamageMultiplier";
    
    // 默认构造函数，直接传入数值即可，已经自带提示栏
    public DamageMultiplierVar(decimal baseValue) : base(DefaultName, baseValue) { }
    
    // 重载构造函数，保留原版高度的灵活性
    public DamageMultiplierVar(string name, decimal baseValue) : base(name, baseValue) { }
}