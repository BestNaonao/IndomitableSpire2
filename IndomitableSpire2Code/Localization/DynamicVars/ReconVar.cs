using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;

/// <summary>
/// 用于侦察机制的动态变量。在 CanonicalVars 中注册并使用即可自带卡牌悬浮窗自动显示机制说明。
/// </summary>
public class ReconVar : DynamicVar
{
    // 设定默认标识符，方便在 json 文件中精准匹配
    public const string DefaultName = "Recon";
    
    // 默认构造函数，直接传入数值即可，已经自带提示栏
    public ReconVar(decimal baseValue) : base(DefaultName, baseValue) { this.WithTooltip(); }
    
    // 重载构造函数，保留原版高度的灵活性
    public ReconVar(string name, decimal baseValue) : base(name, baseValue) { this.WithTooltip(); }
}