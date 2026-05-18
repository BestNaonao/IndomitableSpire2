using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Enums;

public static class IndomitableKeywords
{
    // 舰载机总称
    [CustomEnum("carrier_aircraft"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword CarrierAircraft;
    
    // 战斗攻击机
    [CustomEnum("strike_fighter"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword StrikeFighter;
    
    // 鱼雷轰炸机
    [CustomEnum("torpedo_bomber"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword TorpedoBomber;
    
    // 俯冲轰炸机
    [CustomEnum("dive_bomber"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword DiveBomber;
    
    // 水平轰炸机
    [CustomEnum("level_bomber"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword LevelBomber;
    
    // 耐久：卡牌的耐久，归零时消耗
    [CustomEnum("durability"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Durability;
    
    // 编队：在卡牌末尾显示，代表小队增援机制
    [CustomEnum("formation"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Formation;
    
    // 委托：完成任务，和队友获得奖励
    [CustomEnum("commission"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Commission;
}