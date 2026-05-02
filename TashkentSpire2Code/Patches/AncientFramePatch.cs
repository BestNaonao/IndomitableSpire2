using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TashkentSpire2.TashkentSpire2Code.Cards;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NCard), "Reload")]
public static class AncientFramePatch
{
    static void Postfix(NCard __instance)
    {
        if (__instance?.Model == null) return;

        if (__instance.Model.Rarity != (CardRarity)5)
            return;
        
        if (__instance.Model is not TashkentCard)
            return;
        
        var field = NCardReflectionFields.AncientBorderField;
        if (field == null) return;

        var border = field.GetValue(__instance) as TextureRect;
        if (border == null) return;

        Texture2D? tex = null;
        if (__instance.Model.Type == (CardType)1)
        {
            tex = PreloadManager.Cache.GetAsset<Texture2D>(
                "res://TashkentSpire2/images/card_frames/tashkent_ancient.png");
        }
        else
        {
            tex = PreloadManager.Cache.GetAsset<Texture2D>(
                "res://TashkentSpire2/images/card_frames/tashkent_ancient_power.png");
        }
        

        if (tex != null)
        {
            border.Texture = tex;
        }
    }
}

public static class NCardReflectionFields
{
    public static readonly FieldInfo? AncientTextBgField = typeof(NCard).GetField("_ancientTextBg", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly FieldInfo? AncientBorderField = typeof(NCard).GetField("_ancientBorder", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly FieldInfo? AncientBannerField = typeof(NCard).GetField("_ancientBanner", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly FieldInfo? AncientPortraitField = typeof(NCard).GetField("_ancientPortrait", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly FieldInfo? PortraitField = typeof(NCard).GetField("_portrait", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly FieldInfo? PortraitBorderField = typeof(NCard).GetField("_portraitBorder", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly FieldInfo? FrameField = typeof(NCard).GetField("_frame", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly FieldInfo? BannerField = typeof(NCard).GetField("_banner", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly FieldInfo? TypePlaqueField = typeof(NCard).GetField("_typePlaque", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly FieldInfo? TypeLabelField = typeof(NCard).GetField("_typeLabel", BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly FieldInfo? PortraitCanvasGroupField = typeof(NCard).GetField("_portraitCanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic);
}