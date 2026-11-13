using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Config;
using TashkentSpire2.TashkentSpire2Code.Nodes;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen))]
public static class InjectTashkentSkinSelectPanelPatch
{
	private const string ScenePath = "res://TashkentSpire2/scenes/ui/tashkent_skin_select_panel.tscn";
	private static TashkentSkinSelectPanel? _panelInstance;
	[ThreadStatic] private static StartRunLobby? _lobbySelectingSavedSkin;

	[HarmonyPatch(nameof(NCharacterSelectScreen.SelectCharacter))]
	[HarmonyPrefix]
	public static void SelectCharacterPrefix(NCharacterSelectScreen __instance,
		NCharacterSelectButton charSelectButton, out StartRunLobby? __state)
	{
		__state = _lobbySelectingSavedSkin;
		_lobbySelectingSavedSkin = null;
		TashkentCharacterSelectVoice.Stop();
		if (TashkentConfig.EnableSpineModels
		    && charSelectButton is { IsLocked: false, IsRandom: false }
		    && charSelectButton.Character is TashkentCharacter { CurrentSkin: TashkentSkin.Default })
		{
			_lobbySelectingSavedSkin = __instance.Lobby;
		}
	}

	[HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.SetLocalCharacter), typeof(CharacterModel), typeof(bool))]
	[HarmonyPrefix]
	private static void ApplySavedSkin(StartRunLobby __instance, ref CharacterModel character)
	{
		if (ReferenceEquals(__instance, _lobbySelectingSavedSkin)
		    && character is TashkentCharacter { CurrentSkin: TashkentSkin.Default } defaultSkin)
		{
			_lobbySelectingSavedSkin = null;
			character = TashkentSkinSelectPanel.ResolveLastPlayedSkin(defaultSkin);
		}
	}

	[HarmonyPatch(nameof(NCharacterSelectScreen.SelectCharacter))]
	[HarmonyFinalizer]
	private static void RestoreSelectionScope(StartRunLobby? __state) => _lobbySelectingSavedSkin = __state;

	[HarmonyPatch(nameof(NCharacterSelectScreen.SelectCharacter))]
	[HarmonyPostfix]
	public static void SelectCharacterPostfix(
		NCharacterSelectScreen __instance,
		NCharacterSelectButton charSelectButton)
	{
		if (charSelectButton.IsLocked || charSelectButton.IsRandom
		    || charSelectButton.Character is not TashkentCharacter
		    || __instance.Lobby.LocalPlayer.character is not TashkentCharacter tashkentSkin)
		{
			if (GodotObject.IsInstanceValid(_panelInstance))
				_panelInstance.Visible = false;
			return;
		}

		if (!TashkentConfig.EnableSpineModels)
		{
			if (GodotObject.IsInstanceValid(_panelInstance))
				_panelInstance.Visible = false;
			return;
		}

		var infoPanel = __instance.GetNodeOrNull<Control>("%InfoPanel");
		if (infoPanel is null)
			return;

		if (!GodotObject.IsInstanceValid(_panelInstance) || _panelInstance.GetParent() != infoPanel)
		{
			if (GodotObject.IsInstanceValid(_panelInstance))
				_panelInstance.QueueFreeSafely();

			var scene = ResourceLoader.Load<PackedScene>(ScenePath);
			if (scene is null)
				return;

			_panelInstance = scene.Instantiate<TashkentSkinSelectPanel>(PackedScene.GenEditState.Disabled);
			infoPanel.AddChildSafely(_panelInstance);
			_panelInstance.Position = new Vector2(400f, 0f);
		}

		_panelInstance.SetInteractable(true);
		_panelInstance.ShowAndSync(__instance, tashkentSkin);
	}

	[HarmonyPatch(nameof(NCharacterSelectScreen.MethodName.OnEmbarkPressed))]
	[HarmonyPostfix]
	public static void OnEmbarkPressedPostfix()
	{
		if (GodotObject.IsInstanceValid(_panelInstance))
			_panelInstance.SetInteractable(false);
	}

	[HarmonyPatch(nameof(NCharacterSelectScreen.MethodName.OnUnreadyPressed))]
	[HarmonyPostfix]
	public static void OnUnreadyPressedPostfix()
	{
		if (GodotObject.IsInstanceValid(_panelInstance))
			_panelInstance.SetInteractable(true);
	}

	[HarmonyPatch(nameof(NCharacterSelectScreen.OnSubmenuClosed))]
	[HarmonyPrefix]
	public static void OnSubmenuClosedPrefix() => TashkentCharacterSelectVoice.Stop();
}
