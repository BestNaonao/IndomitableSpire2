using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Config;
using TashkentSpire2.TashkentSpire2Code.Nodes;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

internal static class TashkentSkinSelection
{
    private const string ScenePath = "res://TashkentSpire2/scenes/ui/tashkent_skin_select_panel.tscn";
    private const string PanelName = "TashkentSkinSelectPanel";

    internal static readonly Type[] Screens =
        [typeof(NCharacterSelectScreen), typeof(NBlitzScreen), typeof(NDreadScreen), typeof(NCustomRunScreen)];

    internal static IEnumerable<MethodBase> ScreenMethods(string name) =>
        Screens.Select(type => AccessTools.DeclaredMethod(type, name));

    internal static StartRunLobby? GetLobby(Control screen) => screen switch
    {
        NCharacterSelectScreen standard => standard.Lobby,
        NBlitzScreen blitz => blitz.Lobby,
        NDreadScreen dread => dread.Lobby,
        NCustomRunScreen custom => custom.Lobby,
        _ => null
    };

    private static Control GetPanelParent(Control screen) => screen is NCharacterSelectScreen or NBlitzScreen
        ? screen.GetNodeOrNull<Control>("InfoPanel") ?? screen
        : screen;

    internal static TashkentSkinSelectPanel? GetPanel(Control screen) =>
        GetPanelParent(screen).GetNodeOrNull<TashkentSkinSelectPanel>(PanelName);

    internal static void ShowPanel(Control screen, NCharacterSelectButton? button)
    {
        var panel = GetPanel(screen);
        var lobby = GetLobby(screen);
        if (!TashkentConfig.EnableSpineModels || button is not { IsLocked: false, IsRandom: false }
            || button.Character is not TashkentCharacter || lobby?.LocalPlayer.character is not TashkentCharacter skin)
        {
            panel?.Hide();
            return;
        }

        if (panel == null)
        {
            var scene = ResourceLoader.Load<PackedScene>(ScenePath);
            if (scene == null)
                return;
            panel = scene.Instantiate<TashkentSkinSelectPanel>();
            panel.Name = PanelName;
            var parent = GetPanelParent(screen);
            parent.AddChildSafely(panel);
            if (parent != screen)
                panel.Position = new Vector2(400f, 0f);
        }

        panel.SetInteractable(!lobby.LocalPlayer.isReady);
        panel.ShowAndSync(lobby, skin);
    }
}

[HarmonyPatch]
internal static class InjectTashkentSkinSelectPanelPatch
{
    [ThreadStatic] private static StartRunLobby? _selectionLobby;

    internal static void EnterSelection(StartRunLobby? lobby, out StartRunLobby? previous)
    {
        previous = _selectionLobby;
        _selectionLobby = lobby;
    }

    internal static void ExitSelection(StartRunLobby? previous) => _selectionLobby = previous;

    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        TashkentSkinSelection.ScreenMethods(nameof(NCharacterSelectScreen.SelectCharacter));

    [HarmonyPrefix]
    private static void BeforeSelect(Control __instance, NCharacterSelectButton charSelectButton,
        out StartRunLobby? __state)
    {
        TashkentCharacterSelectVoice.Stop();
        EnterSelection(!charSelectButton.IsLocked ? TashkentSkinSelection.GetLobby(__instance) : null,
            out __state);
    }

    [HarmonyPostfix]
    private static void AfterSelect(Control __instance, NCharacterSelectButton charSelectButton) =>
        TashkentSkinSelection.ShowPanel(__instance, charSelectButton);

    [HarmonyFinalizer]
    private static void RestoreScope(StartRunLobby? __state) => ExitSelection(__state);

    [HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.SetLocalCharacter), typeof(CharacterModel), typeof(bool))]
    internal static class ApplySavedSkin
    {
        [HarmonyPrefix]
        private static void Prefix(StartRunLobby __instance, ref CharacterModel character)
        {
            if (TashkentConfig.EnableSpineModels
                && character is TashkentCharacter { CurrentSkin: TashkentSkin.Default } skin
                && ReferenceEquals(__instance, _selectionLobby))
                character = TashkentSkinSelectPanel.ResolveLastPlayedSkin(skin);
        }
    }
}

[HarmonyPatch]
internal static class TashkentSkinPanelReadyPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        TashkentSkinSelection.ScreenMethods("OnEmbarkPressed")
            .Concat(TashkentSkinSelection.ScreenMethods("OnUnreadyPressed"));

    [HarmonyPostfix]
    private static void Postfix(Control __instance)
    {
        if (TashkentSkinSelection.GetPanel(__instance) is { } panel
            && TashkentSkinSelection.GetLobby(__instance) is { } lobby)
            panel.SetInteractable(!lobby.LocalPlayer.isReady);
    }
}

[HarmonyPatch]
internal static class TashkentSkinPanelClosedPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        TashkentSkinSelection.ScreenMethods("OnSubmenuClosed");

    [HarmonyPrefix]
    private static void Prefix(Control __instance)
    {
        TashkentCharacterSelectVoice.Stop();
        TashkentSkinSelection.GetPanel(__instance)?.Hide();
    }
}

[HarmonyPatch]
internal static class TashkentLobbyPortraitPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        TashkentSkinSelection.ScreenMethods("RefreshButtonSelectionForPlayer")
            .Concat(TashkentSkinSelection.ScreenMethods("RemotePlayerDisconnected"));

    [HarmonyPrefix]
    private static void Prefix(ref StartRunLobbyPlayer player)
    {
        if (player.character is TashkentCharacter { CurrentSkin: not TashkentSkin.Default })
            player.character = ModelDb.Character<TashkentCharacter>();
    }
}

[HarmonyPatch(typeof(NDreadScreen), nameof(NDreadScreen.BeginRotationTickboxChanged))]
internal static class TashkentDreadRotationTickboxPatch
{
    [HarmonyPrefix]
    private static void Prefix(NDreadScreen __instance, out StartRunLobby? __state) =>
        InjectTashkentSkinSelectPanelPatch.EnterSelection(__instance.Lobby, out __state);

    [HarmonyFinalizer]
    private static void Finalizer(StartRunLobby? __state) =>
        InjectTashkentSkinSelectPanelPatch.ExitSelection(__state);
}

[HarmonyPatch(typeof(NDreadScreen), "SetSelectedCharacterVisuals")]
internal static class TashkentDreadSkinPanelPatch
{
    [HarmonyPostfix]
    private static void Postfix(NDreadScreen __instance, CharacterModel characterModel,
        NCharacterSelectButton? ____selectedButton)
    {
        if (characterModel is RotatingCharacter || ____selectedButton?.Character != characterModel)
        {
            TashkentSkinSelection.GetPanel(__instance)?.Hide();
            return;
        }
        TashkentSkinSelection.ShowPanel(__instance, ____selectedButton);
    }
}

[HarmonyPatch(typeof(NDreadScreen), "OnEmbarkPressed")]
internal static class TashkentDreadEmbarkSkinPatch
{
    [HarmonyPrefix]
    private static void Prefix(NDreadScreen __instance, NCharacterSelectButton? ____selectedButton)
    {
        if (!TashkentConfig.EnableSpineModels || ____selectedButton?.Character is not RotatingCharacter
            || __instance.Lobby.LocalPlayer.character is not TashkentCharacter skin)
            return;

        var saved = TashkentSkinSelectPanel.ResolveLastPlayedSkin(ModelDb.Character<TashkentCharacter>());
        if (saved != skin)
            __instance.Lobby.SetLocalCharacter(saved, isRotatingDread: true);
    }
}
