using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization;

/// <summary>卡框、HoverTip 与文本格式化共用的图标定义；顺序即卡框从上到下的机种顺序。</summary>
public sealed record AircraftIcon(string Key, string FormatterName)
{
    private const string Directory = "res://IndomitableSpire2/images/packed/sprite_fonts/";
    public string SmallPath => $"{Directory}{Key}_icon.png";
    public string LargePath => $"{Directory}{Key}_icon_original.png";
    public string InlinePath => $"{Directory}{Key}_inline.tres";
    public string NodeName => $"Indomitable_{Key}_Badge";
    
    // 这里通过 getter 读取 CustomEnum，避免 BaseLib 初始化枚举之前缓存到默认值。
    public CardKeyword Keyword => Key switch
    {
        "carrier_aircraft" => IndomitableKeywords.CarrierAircraft,
        "strike_fighter" => IndomitableKeywords.StrikeFighter,
        "torpedo_bomber" => IndomitableKeywords.TorpedoBomber,
        "dive_bomber" => IndomitableKeywords.DiveBomber,
        "level_bomber" => IndomitableKeywords.LevelBomber,
        _ => throw new InvalidOperationException(Key)
    };
    
    public bool ShouldShowFor(CardModel card) => Key switch
    {
        "carrier_aircraft" => card.IsCarrierAircraft(),
        "strike_fighter" => card.IsAircraftType(IndomitableTags.StrikeFighter, Keyword),
        "torpedo_bomber" => card.IsAircraftType(IndomitableTags.TorpedoBomber, Keyword),
        "dive_bomber" => card.IsAircraftType(IndomitableTags.DiveBomber, Keyword),
        "level_bomber" => card.IsAircraftType(IndomitableTags.LevelBomber, Keyword),
        _ => false
    };
}

public static class AircraftIcons
{
    public static readonly IReadOnlyList<AircraftIcon> All = Array.AsReadOnly<AircraftIcon>([
        new AircraftIcon("carrier_aircraft", "carrierAircraftIcons"),
        new AircraftIcon("strike_fighter", "strikeFighterIcons"),
        new AircraftIcon("torpedo_bomber", "torpedoBomberIcons"),
        new AircraftIcon("dive_bomber", "diveBomberIcons"),
        new AircraftIcon("level_bomber", "levelBomberIcons")
    ]);
    
    public static AircraftIcon? ForKeyword(CardKeyword keyword) => All.FirstOrDefault(icon => icon.Keyword == keyword);
    
    // 与 energyIcons 一样使用原生 [img]：小图保持 40×25，不指定 width/height，不改动源图。
    // inline.tres 仅加左右各 4px 透明边距，得到 48×25 的单一对象（能量 24×24 的双倍宽度）。
    // 每张图片是一个不可拆分的行内对象；U+200B 为相邻图标之间提供无可见空隙的换行机会。
    // MegaLabelHelper 的自动字号测量也会按原图尺寸测量这个完整对象。
    public static string Inline(AircraftIcon icon, int count = 1) =>
        string.Concat(Enumerable.Repeat($"[img]{icon.InlinePath}[/img]\u200B", Math.Max(0, count)));
}