using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using TashkentSpire2.TashkentSpire2Code.Cards;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch]
internal static class TashkentAmmunitionCardVisualPatch
{
    private const string AmmoIconPath = "res://TashkentSpire2/images/combatui/Ammo.png";
    private const string AmmoIconName = "TashkentAmmoIcon";
    private const string AmmoLabelName = "TashkentAmmoLabel";
    private const float AmmoIconSize = 96f;

    private static readonly StringName ShadowOffsetX = "shadow_offset_x";
    private static readonly StringName ShadowOffsetY = "shadow_offset_y";
    private static readonly StringName ShadowOutlineSize = "shadow_outline_size";

    private static readonly Vector2 AmmoIconPosition = new(70f, -254f);
    private static readonly Vector2 AmmoLabelPosition = new(0f, 12f);
    private static readonly Vector2 AmmoLabelSize = new(84f, 84f);

    private static Texture2D? _ammoTexture;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    private static void UpdateVisuals(NCard __instance)
    {
        Refresh(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), "Reload")]
    private static void Reload(NCard __instance)
    {
        Refresh(__instance);
    }

    private static void Refresh(NCard cardNode)
    {
        if (!cardNode.IsNodeReady() || cardNode.Body == null)
        {
            return;
        }

        TextureRect icon = EnsureAmmoIcon(cardNode);
        Label label = EnsureAmmoLabel(icon);

        if (cardNode.Visibility != ModelVisibility.Visible ||
            cardNode.Model is not IAmmunitionCard ammoCard)
        {
            icon.Visible = false;
            return;
        }

        icon.Visible = true;
        icon.Texture = LoadAmmoTexture();

        label.Text = $"{ammoCard.CurrentAmmu}/{ammoCard.MaxAmmu}";

        bool empty = ammoCard.CurrentAmmu == 0;
        bool barrage = cardNode.Model is CardModel card && card.Keywords.Contains(TashkentKeyword.Barrage);

        label.AddThemeColorOverride(
            ThemeConstants.Label.FontColor,
            empty
                ? StsColors.red
                : barrage
                    ? StsColors.gold
                    : StsColors.cream);

        label.AddThemeColorOverride(
            ThemeConstants.Label.FontOutlineColor,
            empty
                ? StsColors.unplayableEnergyCostOutline
                : barrage
                    ? StsColors.rewardLabelGoldOutline
                    : StsColors.defaultStarCostOutline);
    }

    private static TextureRect EnsureAmmoIcon(NCard cardNode)
    {
        TextureRect? icon = cardNode.Body.GetNodeOrNull<TextureRect>(AmmoIconName);
        if (icon != null)
        {
            return icon;
        }

        icon = new TextureRect
        {
            Name = AmmoIconName,
            Position = AmmoIconPosition,
            Size = new Vector2(AmmoIconSize, AmmoIconSize),
            PivotOffset = new Vector2(AmmoIconSize * 0.5f, AmmoIconSize * 0.5f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false
        };

        cardNode.Body.AddChild(icon);

        return icon;
    }

    private static Label EnsureAmmoLabel(TextureRect icon)
    {
        Label? label = icon.GetNodeOrNull<Label>(AmmoLabelName);
        if (label != null)
        {
            return label;
        }

        label = new Label
        {
            Name = AmmoLabelName,
            Position = AmmoLabelPosition,
            Size = AmmoLabelSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Text = string.Empty
        };

        label.AddThemeConstantOverride(ShadowOffsetX, 2);
        label.AddThemeConstantOverride(ShadowOffsetY, 2);
        label.AddThemeConstantOverride(ThemeConstants.Label.OutlineSize, 12);
        label.AddThemeConstantOverride(ShadowOutlineSize, 12);
        label.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, 24);

        Font? font = ResourceLoader.Load<Font>(
            "res://themes/kreon_bold_shared.tres",
            null,
            ResourceLoader.CacheMode.Ignore);

        if (font != null && GodotObject.IsInstanceValid(font))
        {
            label.AddThemeFontOverride(ThemeConstants.Label.Font, font);
        }

        icon.AddChild(label);

        return label;
    }

    private static Texture2D? LoadAmmoTexture()
    {
        if (_ammoTexture == null || !GodotObject.IsInstanceValid(_ammoTexture))
        {
            _ammoTexture = ResourceLoader.Load<Texture2D>(
                AmmoIconPath,
                null,
                ResourceLoader.CacheMode.Ignore);
        }

        return _ammoTexture;
    }
}