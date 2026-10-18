using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Localization;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using SmartFormat;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch]
public static class AircraftIconTextPatches
{
    // GetFormattedText 是很短的包装方法，游戏/JIT 可将其内联，绕过该方法上的 Harmony 补丁。
    // 在实际使用的 SmartFormatter 上注册默认变量解析器，覆盖卡面、能力及直接 SmartFormat 调用。
    // 每次重建 formatter 都重新注册；不修改调用方字典，也不覆盖显式传入的 aircraftPrefix。
    [HarmonyPatch(typeof(LocManager), "LoadLocFormatters")]
    [HarmonyPostfix]
    private static void RegisterDefaultPrefixSource()
    {
        if (Smart.Default.GetSourceExtension<AircraftIconPrefixSource>() == null)
            Smart.Default.AddExtensions(new AircraftIconPrefixSource());
    }
    
    // Postfix 会在 BaseLib 的自定义关键词 Prefix 之后运行，仍使用原版 Title 右侧的 Icon 槽位。
    // 虽然不同于原版的字典缓存方法，每次调用都会重新构造，但由于图片缓存，不一定会带来严重性能问题。
    [HarmonyPatch(typeof(HoverTipFactory), nameof(HoverTipFactory.FromKeyword))]
    [HarmonyPostfix]
    private static void AddKeywordIcon(CardKeyword keyword, ref IHoverTip __result)
    {
        var icon = AircraftIcons.ForKeyword(keyword);
        if (icon == null) return;
        var locKey = "INDOMITABLESPIRE2-" + icon.Key.ToUpperInvariant();
        var description = new LocString("card_keywords", locKey + ".description");
        description.Add("energyPrefix", "");
        __result = new HoverTip(new LocString("card_keywords", locKey + ".title"), description,
            PreloadManager.Cache.GetTexture2D(icon.SmallPath));
    }
    
    // 原版能量是方图，默认 FitWidth 会把横向机型图标挤成方形；按高度等比保留 40:25。
    [HarmonyPatch(typeof(NHoverTipSet), "Init")]
    [HarmonyPostfix]
    private static void PreserveHoverIconAspect(NHoverTipSet __instance) => AdjustIcons(__instance);
    
    private static void AdjustIcons(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is TextureRect { Texture: { } texture } rect &&
                AircraftIcons.All.Any(icon => icon.SmallPath == texture.ResourcePath))
            {
                rect.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
                rect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            }
            AdjustIcons(child);
        }
    }
}