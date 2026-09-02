using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class CardExtensions
{
    private static readonly HashSet<CardKeyword> TargetAircraftKeywords =
    [
        IndomitableKeywords.CarrierAircraft,
        IndomitableKeywords.StrikeFighter,
        IndomitableKeywords.TorpedoBomber,
        IndomitableKeywords.DiveBomber,
        IndomitableKeywords.LevelBomber
    ];
    
    private static readonly HashSet<CardTag> TargetAircraftTags =
    [
        IndomitableTags.CarrierAircraft,
        IndomitableTags.StrikeFighter,
        IndomitableTags.TorpedoBomber,
        IndomitableTags.DiveBomber,
        IndomitableTags.LevelBomber
    ];
    
    private static bool HasAnyAircraftTag(this CardModel card) => card.Tags.Any(TargetAircraftTags.Contains);
    
    private static bool HasAnyAircraftKeyword(this CardModel card) => card.Keywords.Any(TargetAircraftKeywords.Contains);
    
    /// <summary>
    /// 判断卡牌是否原生属于航空分类。该判断只读取静态 Tag，不受后来添加或移除的关键词影响。
    /// </summary>
    public static bool IsNativeCarrierAircraft(this CardModel card) => card.HasAnyAircraftTag();
    
    /// <summary>
    /// 判断卡牌当前是否被视为航空卡牌。任意航空 Tag 或关键词都能使其获得通用航空加成。
    /// </summary>
    public static bool IsCarrierAircraft(this CardModel card) => 
        card.HasAnyAircraftTag() || card.HasAnyAircraftKeyword();
    
    /// <summary>
    /// 判断卡牌是否属于指定航空机种。原生航空卡牌只以 Tag 为准；非原生航空卡牌可以由后来获得的关键词分类。
    /// </summary>
    public static bool IsAircraftType(this CardModel card, CardTag nativeTypeTag, CardKeyword acquiredTypeKeyword) =>
        card.Tags.Contains(nativeTypeTag) || card.Keywords.Contains(acquiredTypeKeyword);
    
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