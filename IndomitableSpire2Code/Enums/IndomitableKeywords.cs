using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Enums;

public static class IndomitableKeywords
{
    // 舰载机总称
    // 航空关键词由右侧徽标展示；None 只禁用卡面自动文字，保留关键词效果和 HoverTip。
    [CustomEnum("carrier_aircraft"), KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword CarrierAircraft;
    
    // 战斗攻击机
    [CustomEnum("strike_fighter"), KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword StrikeFighter;
    
    // 鱼雷轰炸机
    [CustomEnum("torpedo_bomber"), KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword TorpedoBomber;
    
    // 俯冲轰炸机
    [CustomEnum("dive_bomber"), KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword DiveBomber;
    
    // 水平轰炸机
    [CustomEnum("level_bomber"), KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword LevelBomber;
    
    // 耐久：卡牌的耐久，归零时消耗
    [CustomEnum("durability"), KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Durability;
    
    // 需求：打出卡牌的前置条件
    [CustomEnum("require"), KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Require;
    
    // 编队：在卡牌末尾显示，代表小队增援机制
    [CustomEnum("formation"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Formation;
    
    // 回光：将被消耗时立即额外打出一次，由通用补丁处理。在卡面末尾自动显示。
    [CustomEnum("afterglow"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Afterglow;
    
    // 同心：生成或变化的新牌继承来源牌的升级、关键词和附魔。
    [CustomEnum("resonance"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Resonance;
    
    // 委托：完成任务，和队友获得奖励
    [CustomEnum("commission"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Commission;
    
    // 优雅：在一回合内没有受到未格挡伤害，下回合开始时触发
    [CustomEnum("elegance"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Elegance;
    
    // 多用途：根据打出方式或选择，执行不同的效果。通过文本嵌入和 HoverTip 展示。
    [CustomEnum("versatile"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Versatile;
    
    // 机种协同：仅用于卡面文字和悬浮提示，费用变化由舰载机牌自行处理。
    [CustomEnum("type_synergy"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword TypeSynergy;
}