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

[HarmonyPatch]
internal static class AncientBorderMaterialPatch
{
    private const string MaterialPath =
        "res://TashkentSpire2/materials/tashkent_ancient_border_opaque.tres";

    private static readonly ConditionalWeakTable<TextureRect, MaterialOverrideState> MaterialStates = new();

    private static Material? _customMaterial;

    [HarmonyPatch(typeof(NCard), nameof(NCard._Ready))]
    [HarmonyPrefix]
    private static void CaptureSceneMaterial(NCard __instance)
    {
        TextureRect? ancientBorder = __instance.GetNodeOrNull<TextureRect>("%AncientBorder");
        if (ancientBorder != null)
        {
            MaterialStates.GetValue(
                ancientBorder,
                static border => new MaterialOverrideState(border.Material));
        }
    }

    [HarmonyPatch(typeof(NCard), "UpdatePortrait")]
    [HarmonyPostfix]
    private static void ApplyTashkentAncientBorderMaterial(NCard __instance)
    {
        TextureRect? ancientBorder = __instance.GetNodeOrNull<TextureRect>("%AncientBorder");
        if (ancientBorder == null)
        {
            return;
        }

        MaterialOverrideState state = MaterialStates.GetValue(
            ancientBorder,
            static border => new MaterialOverrideState(border.Material));

        if (__instance.Model is not TashkentCard { Rarity: CardRarity.Ancient })
        {
            state.Release(ancientBorder, restoreSceneMaterial: false);
            return;
        }

        Material? material = LoadCustomMaterial();
        if (material == null)
        {
            state.Release(ancientBorder, restoreSceneMaterial: false);
            return;
        }

        state.Apply(ancientBorder, material);
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.OnFreedToPool))]
    [HarmonyPostfix]
    private static void ReleaseTashkentAncientBorderMaterial(NCard __instance)
    {
        TextureRect? ancientBorder = __instance.GetNodeOrNull<TextureRect>("%AncientBorder");
        if (ancientBorder != null && MaterialStates.TryGetValue(ancientBorder, out MaterialOverrideState? state))
        {
            state.Release(ancientBorder, restoreSceneMaterial: true);
        }
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

    private sealed class MaterialOverrideState(Material? sceneMaterial)
    {
        private readonly Material? _sceneMaterial = sceneMaterial;
        private Material? _previousMaterial;
        private Material? _appliedMaterial;
        private bool _ownsCurrentValue;

        public void Apply(TextureRect border, Material material)
        {
            if (!_ownsCurrentValue || !IsSameResource(border.Material, _appliedMaterial))
            {
                _previousMaterial = border.Material;
            }

            border.Material = material;
            _appliedMaterial = material;
            _ownsCurrentValue = true;
        }

        public void Release(TextureRect border, bool restoreSceneMaterial)
        {
            if (!_ownsCurrentValue)
            {
                return;
            }

            if (IsSameResource(border.Material, _appliedMaterial))
            {
                Material? material = restoreSceneMaterial ? _sceneMaterial : _previousMaterial;
                border.Material = IsValidResource(material) ? material : null;
            }

            _previousMaterial = null;
            _appliedMaterial = null;
            _ownsCurrentValue = false;
        }

        private static bool IsSameResource(Material? left, Material? right)
        {
            return ReferenceEquals(left, right) ||
                   IsValidResource(left) &&
                   IsValidResource(right) &&
                   left!.GetInstanceId() == right!.GetInstanceId();
        }

        private static bool IsValidResource(Material? material)
        {
            return material != null && GodotObject.IsInstanceValid(material);
        }
    }
}
