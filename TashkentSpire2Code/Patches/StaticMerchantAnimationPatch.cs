using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using TashkentSpire2.TashkentSpire2Code.Scripts;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

/// <summary>
/// Prevents the game-over screen from sending Spine animations to Tashkent's
/// optional static merchant portrait. The Spine merchant scenes keep using the
/// original method unchanged.
/// </summary>
[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter.PlayAnimation))]
public static class StaticMerchantAnimationPatch
{
	[HarmonyPrefix]
	public static bool SkipAnimationForStaticTashkent(NMerchantCharacter __instance)
	{
		if (__instance is not NMerchantCharacterTashkent)
			return true;

		if (__instance.GetChildCount() == 0)
			return false;

		GodotObject child = __instance.GetChild(0);
		return child.GetClass() == "SpineSprite";
	}
}
