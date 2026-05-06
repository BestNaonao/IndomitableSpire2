using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class CardExtensions
{
    // 纯粹的底层判断：是否有 X 点干劲
    public static bool HasEnoughMotivation(this CardModel card, int amount) => 
        card.CombatState != null && 
        card.Owner.Creature.GetPower<MotivationPower>() is PowerModel power && 
        power.DisplayAmount >= amount;
    
    // 高阶判断：是否满足“需求”变量的数值
    // 注意加入防空判断（card.DynamicVars.HasVar("MotivationRequire")），以防某张卡没注册这个变量却调用了它
    public static bool MeetsMotivationRequirement(this CardModel card) => 
        card.HasEnoughMotivation(card.DynamicVars.MotivationRequire().IntValue);
    
    // 高阶判断：是否足够支付“消耗”变量的数值
    public static bool CanAffordMotivationCost(this CardModel card) => 
        card.HasEnoughMotivation(card.DynamicVars.MotivationConsume().IntValue);
    
    // 终极组合验证：只要卡牌注册了这两种变量，就自动双重验证！
    // 这是最强大的框架方法，以后写卡牌可以直接无脑调用这个
    public static bool ValidateMotivationConditions(this CardModel card)
    {
        var isValid = true;
        
        // 如果卡牌有“需求”变量，验证是否满足
        if (card.DynamicVars.ContainsKey("MotivationRequire"))
            isValid &= card.MeetsMotivationRequirement();
        
        // 如果卡牌有“消耗”变量，验证是否付得起
        if (card.DynamicVars.ContainsKey("MotivationConsume"))
            isValid &= card.CanAffordMotivationCost();
        
        return isValid;
    }
    
    public static bool OutOfDurability(this CardModel card) => 
        card.DynamicVars.Durability().BaseValue <= 0;
}