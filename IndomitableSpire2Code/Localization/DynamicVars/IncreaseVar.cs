using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;

/// <summary>
/// 用于表示卡牌每次成长的增量。
/// </summary>
public class IncreaseVar : DynamicVar
{
    public const string DefaultName = "Increase";
    
    public IncreaseVar(decimal baseValue) : base(DefaultName, baseValue) { }
    
    public IncreaseVar(string name, decimal baseValue) : base(name, baseValue) { }
}