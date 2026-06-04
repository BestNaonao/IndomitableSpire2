using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;

// 干劲获取变量 (比如：获得 10 点干劲)
public sealed class MotivationGainVar(decimal baseValue) 
    : CustomPowerVar<MotivationPower>("MotivationGain", baseValue);

// 干劲需求变量 (比如：需要 50 点干劲)
public sealed class MotivationRequireVar(decimal baseValue) : CustomPowerVar<MotivationPower>(DefaultName, baseValue)
{
    public new const string DefaultName = "MotivationRequire";
    
    // 拦截获取真实修改后的数值
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
        PreviewValue = card.GetActualMotivationCost(IntValue);
    }
}

// 干劲消耗变量 (比如：消耗 25 点干劲)
public sealed class MotivationConsumeVar(decimal baseValue) : CustomPowerVar<MotivationPower>(DefaultName, baseValue)
{
    public new const string DefaultName = "MotivationConsume";
    
    // 拦截获取真实修改后的数值
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
        PreviewValue = card.GetActualMotivationCost(IntValue);
    }
}