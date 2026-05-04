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
    private static AtlasTexture? _vanillaAncientTexture;

    static void Postfix(NCard __instance)
    {
        if (__instance?.Model == null) return;

        var borderField = NCardReflectionFields.AncientBorderField;
        if (borderField == null) return;

        var border = borderField.GetValue(__instance) as TextureRect;
        if (border == null) return;

        bool isTashkent = __instance.Model is TashkentCard;
        bool isAncient = __instance.Model.Rarity == (CardRarity)5;

        if (isAncient && isTashkent)
        {
            string myPath = (__instance.Model.Type == (CardType)1)
                ? "res://TashkentSpire2/images/card_frames/tashkent_ancient.png"
                : "res://TashkentSpire2/images/card_frames/tashkent_ancient_power.png";

            var customTex = PreloadManager.Cache.GetAsset<Texture2D>(myPath);
            if (customTex != null) border.Texture = customTex;
        }
        else
        {
            if (border.Texture != null && border.Texture.ResourcePath.Contains("TashkentSpire2"))
            {
                if (_vanillaAncientTexture == null)
                {
                    var atlas = PreloadManager.Cache.GetAsset<Texture2D>("res://images/atlases/compressed_0.png");
                    if (atlas != null)
                    {
                        _vanillaAncientTexture = new AtlasTexture();
                        _vanillaAncientTexture.Atlas = atlas;

                        _vanillaAncientTexture.Region = new Rect2(1, 1, 821, 1148);
                    }
                }

                if (_vanillaAncientTexture != null)
                {
                    border.Texture = _vanillaAncientTexture;
                }
            }
        }
    }
}

public static class NCardReflectionFields
{
    public static readonly FieldInfo? AncientBorderField = typeof(NCard).GetField("_ancientBorder", BindingFlags.Instance | BindingFlags.NonPublic);
}