using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Nodes;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen))]
internal static class TashkentWorkshopVoteButtonPatch
{
    private const string NodePath = "InfoPanel/TashkentWorkshopVoteButton";
    private const string ScenePath = "res://TashkentSpire2/scenes/ui/tashkent_workshop_vote_button.tscn";

    [HarmonyPrepare]
    private static bool Prepare() => MainFile.EnableWorkshopVote;

    [HarmonyPostfix, HarmonyPatch(nameof(NCharacterSelectScreen.SelectCharacter))]
    private static void SelectCharacterPostfix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton)
    {
        try
        {
            var button = __instance.GetNodeOrNull<TashkentWorkshopVoteButton>(NodePath);
            bool selected = charSelectButton.Character is TashkentCharacter && !charSelectButton.IsLocked && !charSelectButton.IsRandom;
            if (button == null && selected)
            {
                var panel = __instance.GetNodeOrNull<Control>("InfoPanel");
                if (panel == null)
                    return;
                button = ResourceLoader.Load<PackedScene>(ScenePath).Instantiate<TashkentWorkshopVoteButton>();
                panel.AddChildSafely(button);
            }
            button?.SetCharacterSelected(selected);
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn($"Could not initialize workshop vote button: {exception.Message}");
        }
    }

    [HarmonyPrefix, HarmonyPatch(nameof(NCharacterSelectScreen.OnSubmenuOpened))]
    private static void OnOpenedPrefix(NCharacterSelectScreen __instance) =>
        __instance.GetNodeOrNull<TashkentWorkshopVoteButton>(NodePath)?.OpenScreen();

    [HarmonyPrefix, HarmonyPatch(nameof(NCharacterSelectScreen.OnSubmenuClosed))]
    private static void OnClosedPrefix(NCharacterSelectScreen __instance) =>
        __instance.GetNodeOrNull<TashkentWorkshopVoteButton>(NodePath)?.CloseScreen();

    [HarmonyPostfix, HarmonyPatch(nameof(NCharacterSelectScreen.MethodName.OnEmbarkPressed))]
    private static void OnEmbarkPostfix(NCharacterSelectScreen __instance) =>
        __instance.GetNodeOrNull<TashkentWorkshopVoteButton>(NodePath)?.SetInteractable(false);

    [HarmonyPostfix, HarmonyPatch(nameof(NCharacterSelectScreen.MethodName.OnUnreadyPressed))]
    private static void OnUnreadyPostfix(NCharacterSelectScreen __instance) =>
        __instance.GetNodeOrNull<TashkentWorkshopVoteButton>(NodePath)?.SetInteractable(true);
}
