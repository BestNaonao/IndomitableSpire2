using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Config;
using TashkentSpire2.TashkentSpire2Code.Nodes.UI;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

public static class TashkentBattleRewardsPatch
{
    [HarmonyPatch(typeof(NRewardsScreen), nameof(NRewardsScreen._Ready))]
    public static class Ready
    {
        [HarmonyPostfix]
        public static void Postfix(NRewardsScreen __instance, RewardsSet ____rewardsSet, bool ____isTerminal)
        {
            if (!TashkentConfig.EnableCustomBattleRewards || TestMode.IsOn || !____isTerminal || ____rewardsSet.Room is not CombatRoom
                || ____rewardsSet.Player.Character is not TashkentCharacter character)
                return;
            NTashkentBattleRewardsController.Attach(__instance, character);
        }
    }

    [HarmonyPatch(typeof(NRewardsScreen), nameof(NRewardsScreen.AfterOverlayShown))]
    public static class Shown
    {
        [HarmonyPrefix]
        public static void Prefix(NRewardsScreen __instance) =>
            NTashkentBattleRewardsController.Find(__instance)?.ShowPresentation();

        [HarmonyPostfix]
        public static void Postfix(NRewardsScreen __instance, ref Tween? ____fadeTween) =>
            NTashkentBattleRewardsController.Find(__instance)?.AnimatePresentation(ref ____fadeTween);
    }

    [HarmonyPatch(typeof(NRewardsScreen), nameof(NRewardsScreen.AfterOverlayHidden))]
    public static class Hidden
    {
        [HarmonyPostfix]
        public static void Postfix(NRewardsScreen __instance) =>
            NTashkentBattleRewardsController.Find(__instance)?.HidePresentation();
    }

    [HarmonyPatch(typeof(NProceedButton), "get_ShowPos")]
    public static class ProceedShowPosition
    {
        [HarmonyPrefix]
        public static bool Prefix(NProceedButton __instance, ref Vector2 __result) =>
            TryPosition(__instance, false, ref __result);
    }

    [HarmonyPatch(typeof(NProceedButton), "get_HidePos")]
    public static class ProceedHidePosition
    {
        [HarmonyPrefix]
        public static bool Prefix(NProceedButton __instance, ref Vector2 __result) =>
            TryPosition(__instance, true, ref __result);
    }

    private static bool TryPosition(NProceedButton button, bool hidden, ref Vector2 result)
    {
        if (button.GetParent() is not NRewardsScreen screen
            || NTashkentBattleRewardsController.Find(screen) is not { } presentation)
            return true;
        result = presentation.ProceedPosition(hidden);
        return false;
    }
}
