using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Config;
using TashkentSpire2.TashkentSpire2Code.Nodes;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

internal static class TashkentCharacterRoster
{
    internal static bool IsSkinVariant(CharacterModel character) =>
        character is TashkentCharacter { CurrentSkin: not TashkentSkin.Default };
}

[HarmonyPatch]
internal static class TashkentCharacterRosterScopePatch
{
    [ThreadStatic] internal static bool HideVariants;

    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        [
            AccessTools.DeclaredMethod(typeof(ProgressSaveManager), "UpdateEpochsPostRun"),
            AccessTools.DeclaredMethod(typeof(StartRunLobby), "BeginRunLocally"),
            AccessTools.DeclaredMethod(typeof(NCharacterSelectScreen), "InitCharacterButtons"),
            AccessTools.DeclaredMethod(typeof(NCharacterSelectScreen), "UpdateRandomCharacterVisibility"),
            AccessTools.DeclaredMethod(typeof(NBlitzScreen), "InitCharacterButtons"),
            AccessTools.DeclaredMethod(typeof(NBlitzScreen), "UpdateRandomCharacterVisibility"),
            AccessTools.DeclaredMethod(typeof(NDreadScreen), "InitCharacterButtons"),
            AccessTools.DeclaredMethod(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.InitAsRandom)),
            AccessTools.DeclaredMethod(typeof(NCustomRunScreen), "InitCharacterButtons"),
            AccessTools.DeclaredMethod(typeof(NCustomRunModifiersList), nameof(NCustomRunModifiersList._Ready)),
            AccessTools.DeclaredMethod(typeof(NCustomRunScreen), "OnRandomizePressed"),
            AccessTools.DeclaredMethod(typeof(NDailyRunScreen), "SetupLobbyParams")
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

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
[HarmonyAfter("BaseLib")]
internal static class TashkentBaseLibCharacterRosterPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static IEnumerable<CharacterModel> Postfix(IEnumerable<CharacterModel> __result)
    {
        if (TashkentDreadRotationIdPatch.RotationCharacter is { } skin)
            return __result.Where(character => !TashkentCharacterRoster.IsSkinVariant(character))
                .Select(character => character is TashkentCharacter ? skin : character).ToArray();

        return !TashkentCharacterCatalogPatch.IncludeSkinModels || TashkentCharacterRosterScopePatch.HideVariants
            ? __result.Where(character => !TashkentCharacterRoster.IsSkinVariant(character)).ToArray()
            : __result;
    }
}

[HarmonyPatch(typeof(AscensionHelper), nameof(AscensionHelper.GetRotatingCharacterIdForStreak))]
internal static class TashkentDreadRotationIdPatch
{
    private readonly record struct RotationState(TashkentCharacter? PreviousCharacter);

    [ThreadStatic] internal static TashkentCharacter? RotationCharacter;

    [HarmonyPrefix]
    private static void Prefix(ref ModelId startingCharacter, out RotationState? __state)
    {
        __state = new RotationState(RotationCharacter);
        var defaultCharacter = ModelDb.Character<TashkentCharacter>();
        RotationCharacter = TashkentConfig.EnableSpineModels
            ? TashkentSkinSelectPanel.ResolveLastPlayedSkin(defaultCharacter) : defaultCharacter;
        if (TashkentSharedProgressionPatch.GetProgressionId(startingCharacter) == defaultCharacter.Id)
            startingCharacter = RotationCharacter.Id;
    }

    [HarmonyFinalizer]
    private static void Finalizer(RotationState? __state)
    {
        if (__state.HasValue)
            RotationCharacter = __state.Value.PreviousCharacter;
    }
}

[HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
internal static class TashkentHideSkinButtonsPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCharacterSelectButton __instance, CharacterModel character,
        ICharacterSelectButtonDelegate del)
    {
        if (del is NCharacterSelectScreen or NBlitzScreen or NDreadScreen or NCustomRunScreen
            && TashkentCharacterRoster.IsSkinVariant(character))
            __instance.Hide();
    }
}

[HarmonyPatch(typeof(ModifierModel), nameof(ModifierModel.Pick2Good1Bad))]
internal static class TashkentCustomRunRandomModifiersPatch
{
    [HarmonyPrefix]
    private static void Prefix(ref IEnumerable<CharacterModel> excludedCharacters)
    {
        if (TashkentCharacterRosterScopePatch.HideVariants)
            excludedCharacters = excludedCharacters.Select(character => TashkentCharacterRoster.IsSkinVariant(character)
                ? ModelDb.Character<TashkentCharacter>() : character);
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

    [HarmonyPostfix, HarmonyPatch(nameof(NDreadScreen.OnSubmenuOpened))]
    private static void AfterOpened(NDreadScreen __instance, List<NCharacterSelectButton> ____charSelectButtons,
        ref CharacterModel? ____rotatingPostAnimationCharacterSelect, ref Control? ____overriddenFocusedControl)
    {
        bool singleplayer = __instance.Lobby.NetService.Type == NetGameType.Singleplayer;
        var progress = SaveManager.Instance.Progress;
        var pending = singleplayer ? progress.PendingSingleplayerDreadWinStreakResult
            : progress.PendingMultiplayerDreadWinStreakResult;
        if (pending is not { WasReset: true } || pending.CharacterId != ModelDb.GetId<RotatingCharacter>())
            return;

        if (____rotatingPostAnimationCharacterSelect is TashkentCharacter)
            ____rotatingPostAnimationCharacterSelect = ModelDb.Character<TashkentCharacter>();

        var preferredId = singleplayer ? progress.PreferredDreadCharacter : progress.PreferredMultiplayerDreadCharacter;
        var defaultId = ModelDb.GetId<TashkentCharacter>();
        if (pending.WinStreakBeforeReset != 0 || preferredId == defaultId
            || TashkentSharedProgressionPatch.GetProgressionId(preferredId) != defaultId)
            return;

        if (singleplayer)
            progress.PreferredDreadCharacter = defaultId;
        else
            progress.PreferredMultiplayerDreadCharacter = defaultId;
        ____overriddenFocusedControl = ____charSelectButtons.FirstOrDefault(button =>
            !button.IsLocked && button.Visible && button.Character.Id == defaultId);
    }
}

[HarmonyPatch]
internal static class TashkentCharacterPreferencePatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
    [
        AccessTools.DeclaredMethod(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuOpened)),
        AccessTools.DeclaredMethod(typeof(NBlitzScreen), nameof(NBlitzScreen.OnSubmenuOpened)),
        AccessTools.DeclaredMethod(typeof(NDreadScreen), nameof(NDreadScreen.OnSubmenuOpened))
    ];

    [HarmonyPrefix]
    private static void NormalizeOldPreferences()
    {
        var progress = SaveManager.Instance.Progress;
        progress.PendingCharacterUnlock = TashkentSharedProgressionPatch.GetProgressionId(progress.PendingCharacterUnlock);
        progress.PreferredStandardCharacter = TashkentSharedProgressionPatch.GetProgressionId(progress.PreferredStandardCharacter);
        progress.PreferredMultiplayerStandardCharacter = TashkentSharedProgressionPatch.GetProgressionId(progress.PreferredMultiplayerStandardCharacter);
        progress.PreferredBlitzCharacter = TashkentSharedProgressionPatch.GetProgressionId(progress.PreferredBlitzCharacter);
        progress.PreferredMultiplayerBlitzCharacter = TashkentSharedProgressionPatch.GetProgressionId(progress.PreferredMultiplayerBlitzCharacter);
        progress.PreferredDreadCharacter = TashkentSharedProgressionPatch.GetProgressionId(progress.PreferredDreadCharacter);
        progress.PreferredMultiplayerDreadCharacter = TashkentSharedProgressionPatch.GetProgressionId(progress.PreferredMultiplayerDreadCharacter);
    }
}
