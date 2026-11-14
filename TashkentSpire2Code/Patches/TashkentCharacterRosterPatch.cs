using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

internal static class TashkentCharacterRoster
{
    internal static bool IsSkinVariant(CharacterModel character) =>
        character is TashkentCharacter { CurrentSkin: not TashkentSkin.Default };
}

// BaseLib supplies these models through its AllCharacters postfix. Scope the
// filter to the consumers that count characters or roll a random character.
[HarmonyPatch]
internal static class TashkentCharacterRosterScopePatch
{
    [ThreadStatic] internal static bool HideVariants;

    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        [
            AccessTools.DeclaredMethod(typeof(ProgressSaveManager), "UpdateEpochsPostRun"),
            AccessTools.DeclaredMethod(typeof(StartRunLobby), "BeginRunLocally"),
            AccessTools.DeclaredMethod(typeof(AscensionHelper), nameof(AscensionHelper.GetRotatingCharacterIdForStreak))
        ];

    [HarmonyPrefix]
    private static void Prefix(out bool __state)
    {
        __state = HideVariants;
        HideVariants = true;
    }

    [HarmonyFinalizer]
    private static void Finalizer(bool __state) => HideVariants = __state;
}

[HarmonyPatch]
internal static class TashkentBaseLibCharacterRosterPatch
{
    private static MethodInfo? BaseLibRosterPostfix() => AccessTools.DeclaredMethod(
        AccessTools.TypeByName("BaseLib.Patches.Content.AddCustomCharacters"), "Patch");

    [HarmonyPrepare]
    private static bool Prepare() => BaseLibRosterPostfix() != null;

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod() => BaseLibRosterPostfix();

    [HarmonyPostfix]
    private static IEnumerable<CharacterModel> Postfix(IEnumerable<CharacterModel> __result) =>
        TashkentCharacterRosterScopePatch.HideVariants
            ? __result.Where(character => !TashkentCharacterRoster.IsSkinVariant(character)).ToArray()
            : __result;
}

[HarmonyPatch(typeof(AscensionHelper), nameof(AscensionHelper.GetRotatingCharacterIdForStreak))]
internal static class TashkentDreadRotationIdPatch
{
    [HarmonyPrefix]
    private static void Prefix(ref ModelId startingCharacter) =>
        startingCharacter = TashkentSharedProgressionPatch.GetProgressionId(startingCharacter);
}

[HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
internal static class TashkentHideSkinButtonsPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCharacterSelectButton __instance, CharacterModel character,
        ICharacterSelectButtonDelegate del)
    {
        if (del is NBlitzScreen or NDreadScreen or NCustomRunScreen
            && TashkentCharacterRoster.IsSkinVariant(character))
            __instance.Hide();
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), "InitCharacterButtons")]
internal static class TashkentCustomRunFocusPatch
{
    [HarmonyPostfix]
    private static void Postfix(Control ____charButtonContainer)
    {
        var buttons = ____charButtonContainer.GetChildren().OfType<NCharacterSelectButton>()
            .Where(button => button.Visible).ToArray();
        for (int index = 0; index < buttons.Length; index++)
        {
            buttons[index].FocusNeighborLeft = buttons[Math.Max(0, index - 1)].GetPath();
            buttons[index].FocusNeighborRight = buttons[Math.Min(buttons.Length - 1, index + 1)].GetPath();
        }
    }
}

[HarmonyPatch(typeof(NDreadScreen))]
internal static class TashkentDreadProgressPatch
{
    [HarmonyPrefix, HarmonyPatch("UpdateProgressForRotation")]
    private static void BeforeSave(ref CharacterModel embarkingCharacter)
    {
        if (embarkingCharacter is TashkentCharacter)
            embarkingCharacter = ModelDb.Character<TashkentCharacter>();
    }

    [HarmonyPrefix, HarmonyPatch(nameof(NDreadScreen.OnSubmenuOpened))]
    private static void NormalizeOldPreference(NDreadScreen __instance)
    {
        var progress = SaveManager.Instance.Progress;
        if (__instance.Lobby.NetService.Type == NetGameType.Singleplayer)
            progress.PreferredDreadCharacter = TashkentSharedProgressionPatch.GetProgressionId(progress.PreferredDreadCharacter);
        else
            progress.PreferredMultiplayerDreadCharacter = TashkentSharedProgressionPatch.GetProgressionId(progress.PreferredMultiplayerDreadCharacter);
    }
}
