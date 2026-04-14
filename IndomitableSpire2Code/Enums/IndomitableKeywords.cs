using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Enums;

public static class IndomitableKeywords
{
    // 舰载机总称
    [CustomEnum("carrier_aircraft"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword CarrierAircraft;
    
    // 战斗机
    [CustomEnum("fighter"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Fighter;
    
    // 攻击机
    [CustomEnum("attacker"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Attacker;
    
    // 鱼雷轰炸机
    [CustomEnum("torpedo_bomber"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword TorpedoBomber;
    
    // 俯冲轰炸机
    [CustomEnum("dive_bomber"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword DiveBomber;
    
    // 水平轰炸机
    [CustomEnum("level_bomber"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword LevelBomber;
    
    // 跳弹轰炸机
    [CustomEnum("skip_bomber"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword SkipBomber;
    
    // 编队：在卡牌末尾显示，代表小队增援机制
    [CustomEnum("formation"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Formation;
    
    // 耐久度
    [CustomEnum("durability"), KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Durability;
}