using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class CardExtensions
{
    /// <summary>
    /// 判断卡牌是否原生属于舰载机分类。该判断只读取静态 Tag，不受后来添加的关键词影响。
    /// </summary>
    public static bool IsNativeCarrierAircraft(this CardModel? card) =>
        card?.Tags.Contains(IndomitableTags.CarrierAircraft) == true;
    
    /// <summary>
    /// 判断卡牌当前是否被视为舰载机，包括原生舰载机和后来获得舰载机关键词的卡牌。
    /// </summary>
    public static bool IsCarrierAircraft(this CardModel? card) => 
        card != null && (card.IsNativeCarrierAircraft() || card.Keywords.Contains(IndomitableKeywords.CarrierAircraft));
    
    // ==========================================================
    // 耐久度系统判定扩展
    // ==========================================================
    
    // 1. 安全判定：这张卡牌是否注册了耐久度相关的变量？
    public static bool HasDurability(this CardModel card) => 
        card.DynamicVars.ContainsKey(DurabilityVar.DefaultName) && 
        card.DynamicVars.ContainsKey(MaxDurabilityVar.DefaultName);
    
    // 2. 检查是否耗尽（加入安全判定）
    public static bool OutOfDurability(this CardModel card) => 
        card.HasDurability() && card.DynamicVars.Durability().BaseValue <= 0;
    
    // 3. 检查是否满耐久（如果没有耐久变量，默认视为满状态，不需要维修）
    public static bool IsFullDurability(this CardModel card) => 
        !card.HasDurability() || card.DynamicVars.Durability().BaseValue >= card.DynamicVars.MaxDurability().BaseValue;
    
    // 4. 底层数据修改：恢复指定的耐久值
    public static void Repair(this CardModel card, int amount)
    {
        if (amount <= 0 || !card.HasDurability()) return;
        card.DynamicVars.Durability().BaseValue = 
            Math.Min(card.DynamicVars.MaxDurability().BaseValue, card.DynamicVars.Durability().BaseValue + amount);
    }
}