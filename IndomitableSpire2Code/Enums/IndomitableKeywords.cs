using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Enums;

public static class IndomitableKeywords
{
    // 舰载机总称
    [CustomEnum, KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword CarrierAircraft;

    // 战斗机
    [CustomEnum, KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Fighter;

    // 攻击机
    [CustomEnum, KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Attacker;

    // 鱼雷轰炸机
    [CustomEnum, KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword TorpedoBomber;

    // 俯冲轰炸机
    [CustomEnum, KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword DiveBomber;

    // 水平轰炸机
    [CustomEnum, KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword LevelBomber;

    // 跳弹轰炸机
    [CustomEnum, KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword SkipBomber;
}