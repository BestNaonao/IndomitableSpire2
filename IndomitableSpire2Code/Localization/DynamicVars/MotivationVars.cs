using IndomitableSpire2.IndomitableSpire2Code.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;

// 干劲获取变量 (比如：获得 10 点干劲)
public sealed class MotivationGainVar(decimal baseValue) 
    : CustomPowerVar<MotivationPower>("MotivationGain", baseValue);

// 干劲需求变量 (比如：需要 50 点干劲)
public sealed class MotivationRequireVar(decimal baseValue)
    : CustomPowerVar<MotivationPower>("MotivationRequire", baseValue);

// 干劲消耗变量 (比如：消耗 25 点干劲)
public sealed class MotivationConsumeVar(decimal baseValue) 
    : CustomPowerVar<MotivationPower>("MotivationConsume", baseValue);