using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace TashkentSpire2.TashkentSpire2Code.Keywords;

public class TashkentKeyword
{
    [CustomEnum("Barrage")]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Barrage;

    [CustomEnum("Choice")]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Choice;
}
