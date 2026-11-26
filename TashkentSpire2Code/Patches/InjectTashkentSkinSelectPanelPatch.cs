using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
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
using MegaCrit.Sts2.Core.Runs;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Config;
using TashkentSpire2.TashkentSpire2Code.Nodes;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

internal static class TashkentSkinSelection
{
    private const string ScenePath = "res://TashkentSpire2/scenes/ui/tashkent_skin_select_panel.tscn";
    private const string PanelName = "TashkentSkinSelectPanel";
    private static readonly AccessTools.FieldRef<NDailyRunScreen, StartRunLobby?> DailyLobby =
        AccessTools.FieldRefAccess<NDailyRunScreen, StartRunLobby?>("_lobby");

    internal static readonly Type[] Screens =
        [typeof(NCharacterSelectScreen), typeof(NBlitzScreen), typeof(NDreadScreen), typeof(NCustomRunScreen)];

    internal static IEnumerable<MethodBase> ScreenMethods(string name) =>
        Screens.Select(type => AccessTools.DeclaredMethod(type, name));

    internal static IEnumerable<MethodBase> LifecycleMethods(string name) =>
        Screens.Append(typeof(NDailyRunScreen)).Select(type => AccessTools.DeclaredMethod(type, name));

    internal static StartRunLobby? GetLobby(Control screen) => screen switch
    {
        NCharacterSelectScreen standard => standard.Lobby,
        NBlitzScreen blitz => blitz.Lobby,
        NDreadScreen dread => dread.Lobby,
        NCustomRunScreen custom => custom.Lobby,
        NDailyRunScreen daily => DailyLobby(daily),
        _ => null
    };

    private static Control GetPanelParent(Control screen) => screen switch
    {
        NCharacterSelectScreen or NBlitzScreen => screen.GetNodeOrNull<Control>("InfoPanel") ?? screen,
        NCustomRunScreen => screen.GetNodeOrNull<Control>("LeftContainer") ?? screen,
        _ => screen
    };

    internal static TashkentSkinSelectPanel? GetPanel(Control screen) =>
        GetPanelParent(screen).GetNodeOrNull<TashkentSkinSelectPanel>(PanelName);

    internal static void ShowPanel(Control screen, NCharacterSelectButton? button, CharacterModel? displayedCharacter = null)
    {
        var panel = GetPanel(screen);
        var lobby = GetLobby(screen);
        bool isDreadRotation = screen is NDreadScreen && button?.Character is RotatingCharacter;
        if (!TashkentConfig.EnableSpineModels || button is not { IsLocked: false }
            || (button.IsRandom && !isDreadRotation)
            || (displayedCharacter ?? button.Character) is not TashkentCharacter
            || lobby?.LocalPlayer.character is not TashkentCharacter skin)
        {
            panel?.Hide();
            return;
        }

        ShowPanel(screen, lobby, skin);
    }

    internal static void ShowDailyPanel(NDailyRunScreen screen, StartRunLobby? lobby)
    {
        if (!TashkentConfig.EnableSpineModels || lobby?.LocalPlayer.character is not TashkentCharacter skin)
        {
            GetPanel(screen)?.Hide();
            return;
        }
        ShowPanel(screen, lobby, skin);
    }

    private static void ShowPanel(Control screen, StartRunLobby lobby, TashkentCharacter skin)
    {
        var panel = GetPanel(screen);
        if (panel == null)
        {
            var scene = ResourceLoader.Load<PackedScene>(ScenePath);
            if (scene == null)
                return;
            panel = scene.Instantiate<TashkentSkinSelectPanel>();
            panel.Name = PanelName;
            var parent = GetPanelParent(screen);
            parent.AddChildSafely(panel);
            if (screen is NDreadScreen)
                panel.SetScreenLayout(new Vector2(0.5f, 0.5f), centerOffset: new Vector2(613f, 22f));
            else if (screen is not NCustomRunScreen && parent != screen)
                panel.Position = new Vector2(400f, 0f);
        }

        if (screen is NDailyRunScreen)
            panel.SetCompactScreenLayout(new Vector2(0.5f, 0.5f), new Vector2(10f, -90f));
        else if (screen is NCustomRunScreen)
        {
            if (lobby.NetService.Type.IsMultiplayer())
                panel.SetCompactScreenLayout(Vector2.Zero, new Vector2(135f, 365f), narrow: true);
            else if (screen.GetNodeOrNull<Control>("%SeedInput") is { } seedInput)
                panel.PlaceAboveSeed(seedInput);
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
    private static void AfterSelect(Control __instance, NCharacterSelectButton charSelectButton)
    {
        if (__instance is not NDreadScreen)
            TashkentSkinSelection.ShowPanel(__instance, charSelectButton);
    }

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
                character = __instance.GameMode == GameMode.Daily && __instance.LocalPlayer.character is TashkentCharacter selected
                    ? selected : TashkentSkinSelectPanel.ResolveLastPlayedSkin(skin);
        }
    }
}

[HarmonyPatch]
internal static class TashkentSkinPanelReadyPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        TashkentSkinSelection.LifecycleMethods("OnEmbarkPressed")
            .Concat(TashkentSkinSelection.LifecycleMethods("OnUnreadyPressed"));

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
        TashkentSkinSelection.LifecycleMethods("OnSubmenuClosed");

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

[HarmonyPatch]
internal static class TashkentDreadSkinPanelPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
    [
        AccessTools.DeclaredMethod(typeof(NDreadScreen), nameof(NDreadScreen.SelectCharacter)),
        AccessTools.DeclaredMethod(typeof(NDreadScreen), "SetSelectedCharacterVisuals")
    ];

    [HarmonyPostfix]
    private static void Postfix(NDreadScreen __instance, NCharacterSelectButton? ____selectedButton,
        CharacterModel? ____rotatingDisplayCharacterOverride, CharacterModel? ____rotatingCharacter,
        bool ____doingChainAnimation)
    {
        var selected = ____selectedButton?.Character;
        var displayed = selected is RotatingCharacter
            ? ____rotatingDisplayCharacterOverride ?? ____rotatingCharacter
            : selected;
        if (selected is RotatingCharacter && ____doingChainAnimation)
        {
            TashkentSkinSelection.GetPanel(__instance)?.Hide();
            return;
        }
        TashkentSkinSelection.ShowPanel(__instance, ____selectedButton, displayed);
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
