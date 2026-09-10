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
    
    // ==========================================================
    // 同心关键词继承扩展
    // ==========================================================
    
    /// <summary>
    /// 只复制同心指定的三项属性，不复制卡牌身份、牌堆、费用修正或其它战斗状态。
    /// 同时用于尚未入堆的实体和选择界面的预览，不触发升级/附魔的界面与永久牌组记录。
    /// </summary>
    public static void InheritResonanceFrom(this CardModel card, CardModel source)
    {
        if (card == source) return; // 防止自己复制自己。
        card.AssertMutable();
        // 1. 与 CardModel.DeepCloneFields 一致：全局光环关键词由各张牌自行计算。克隆附魔。计算升级等级。
        var keywords = source.GetKeywordsWithSources(KeywordSources.Local).ToHashSet();
        var enchantment = (EnchantmentModel?)source.Enchantment?.ClonePreservingMutability();
        var upgradeLevel = Math.Min(source.CurrentUpgradeLevel, card.MaxUpgradeLevel);
        // 2. 清理旧有附魔。
        card.ClearEnchantmentInternal();
        // 3. 调整升级等级。先降级回基础无升级，再升级至相同等级。
        if (card.CurrentUpgradeLevel > upgradeLevel)
            card.DowngradeInternal();
        while (card.CurrentUpgradeLevel < upgradeLevel && card.IsUpgradable)
            card.UpgradeInternal();
        // 4. 如果来源有附魔，则给目标卡牌施加该附魔。
        if (enchantment != null)
        {
            // 绑定独立附魔实例，并应用附魔对卡牌数值的修改。
            // 不用 CanEnchant 重新筛选（例如 SoulsPower 的来源已经移除了 Exhaust）。
            card.EnchantInternal(enchantment, enchantment.Amount);
            // 新牌保留自己的数值，因此需施加附魔对新牌的修改。
            enchantment.ModifyCard();
        }
        // 5. 放在升级和附魔之后，保留来源最终的关键词集合，包括已移除的关键词。
        foreach (var keyword in card.GetKeywordsWithSources(KeywordSources.Local).Except(keywords).ToArray())
            card.RemoveKeyword(keyword);
        foreach (var keyword in keywords.Except(card.GetKeywordsWithSources(KeywordSources.Local)).ToArray())
            card.AddKeyword(keyword);
        // 6. 清理升级预览状态，确保卡牌在 UI 上正常显示。
        card.FinalizeUpgradeInternal();
    }
}