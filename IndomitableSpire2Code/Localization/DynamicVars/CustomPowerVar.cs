using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;

// 自定义能力变量，可以自动添加提示框
public class CustomPowerVar<T> : PowerVar<T> where T : PowerModel
{
    public static readonly string DefaultName = typeof(T).Name;
    
    public CustomPowerVar(decimal baseValue) : base(DefaultName, baseValue)
    {
        this.WithPowerTooltip<CustomPowerVar<T>, T>();
    }
    
    public CustomPowerVar(string name, decimal baseValue) : base(name, baseValue)
    {
        this.WithPowerTooltip<CustomPowerVar<T>, T>();
    }
}