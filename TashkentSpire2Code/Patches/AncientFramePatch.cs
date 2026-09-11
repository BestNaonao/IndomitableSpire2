using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TashkentSpire2.TashkentSpire2Code.Cards;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.AncientBorder), MethodType.Getter)]
internal static class AncientFramePatch
{
    private const string AncientPath =
        "res://TashkentSpire2/images/card_frames/tashkent_ancient.png";

    private const string AncientPowerPath =
        "res://TashkentSpire2/images/card_frames/tashkent_ancient_power.png";

    [HarmonyPrefix]
    private static bool UseTashkentAncientBorder(CardModel __instance, ref Texture2D __result)
    {
        if (__instance is not TashkentCard || __instance.Rarity != CardRarity.Ancient)
        {
            return true;
        }

        string path = __instance.Type == CardType.Attack
            ? AncientPath
            : AncientPowerPath;

        Texture2D? texture = ResourceLoader.Load<Texture2D>(
            path,
            null,
            ResourceLoader.CacheMode.Reuse);
        if (texture == null)
        {
            return true;
        }

        __result = texture;
        return false;
    }
}

[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
internal static class AncientBorderMaterialPatch
{
    private const string MaterialPath =
        "res://TashkentSpire2/materials/tashkent_ancient_border_opaque.tres";

    private static readonly ConditionalWeakTable<TextureRect, OriginalMaterial> OriginalMaterials = new();

    private static Material? _customMaterial;

    [HarmonyPostfix]
    private static void ApplyTashkentAncientBorderMaterial(NCard __instance)
    {
        TextureRect? ancientBorder = __instance.GetNodeOrNull<TextureRect>("%AncientBorder");
        if (ancientBorder == null)
        {
            return;
        }

        OriginalMaterial original = OriginalMaterials.GetValue(
            ancientBorder,
            static border => new OriginalMaterial(border.Material));

        if (__instance.Model is not TashkentCard { Rarity: CardRarity.Ancient })
        {
            ancientBorder.Material = original.Value;
            return;
        }

        ancientBorder.Material = LoadCustomMaterial() ?? original.Value;
    }

    private static Material? LoadCustomMaterial()
    {
        if (_customMaterial == null || !GodotObject.IsInstanceValid(_customMaterial))
        {
            _customMaterial = ResourceLoader.Load<Material>(
                MaterialPath,
                null,
                ResourceLoader.CacheMode.Reuse);
        }

        return _customMaterial;
    }

    private sealed class OriginalMaterial(Material? value)
    {
        public Material? Value { get; } = value;
    }
}
