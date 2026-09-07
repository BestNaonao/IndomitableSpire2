using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Nodes;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen))]
public static class InjectTashkentSkinSelectPanelPatch
{
	private const string ScenePath = "res://TashkentSpire2/scenes/ui/tashkent_skin_select_panel.tscn";
	private static TashkentSkinSelectPanel? _panelInstance;

	[HarmonyPatch(nameof(NCharacterSelectScreen.SelectCharacter))]
	[HarmonyPostfix]
	public static void SelectCharacterPostfix(
		NCharacterSelectScreen __instance,
		CharacterModel characterModel)
	{
		if (characterModel is not TashkentCharacter tashkentSkin)
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
}
