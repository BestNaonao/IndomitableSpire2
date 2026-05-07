using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;

// 干劲获取变量 (比如：获得 10 点干劲)
public sealed class MotivationGainVar(decimal baseValue) 
    : PowerVar<MotivationPower>("MotivationGain", baseValue);

// 干劲需求变量 (比如：需要 50 点干劲)
public sealed class MotivationRequireVar(decimal baseValue)
    : DynamicVar("MotivationRequire", baseValue);

// 干劲消耗变量 (比如：消耗 25 点干劲)
public sealed class MotivationConsumeVar(decimal baseValue) 
    : DynamicVar("MotivationConsume", baseValue);