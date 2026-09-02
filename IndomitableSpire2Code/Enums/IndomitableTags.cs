using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Enums;

public static class IndomitableTags
{
    // 用于代码逻辑检测：if (card.Tags.Contains(IndomitableTags.Fighter)) ...
    // 一般来说，Tag 是卡牌的原生属性，在战斗中一般不发生变化
    [CustomEnum] public static CardTag CarrierAircraft;
    [CustomEnum] public static CardTag StrikeFighter;
    [CustomEnum] public static CardTag TorpedoBomber;
    [CustomEnum] public static CardTag DiveBomber;
    [CustomEnum] public static CardTag LevelBomber;
}