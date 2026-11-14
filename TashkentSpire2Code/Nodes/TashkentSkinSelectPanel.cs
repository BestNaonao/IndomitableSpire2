using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Config;

namespace TashkentSpire2.TashkentSpire2Code.Nodes;

[GlobalClass]
public partial class TashkentSkinSelectPanel : Control
{
	private static readonly TashkentCharacter[] Skins =
	[
		ModelDb.Character<TashkentCharacter>(),
		ModelDb.Character<TashkentVariantTwo>(),
		ModelDb.Character<TashkentVariantThree>(),
		ModelDb.Character<TashkentVariantFour>(),
		ModelDb.Character<TashkentMu>()
	];

	internal static TashkentCharacter ResolveLastPlayedSkin(TashkentCharacter fallback)
		=> Array.Find(Skins, skin => skin.Id.Entry == TashkentConfig.LastPlayedSkinId) ?? fallback;

	private TextureButton _leftArrow = null!;
	private TextureButton _rightArrow = null!;
	private Control _visualContainer = null!;
	private Label _skinNameLabel = null!;
	private StartRunLobby _lobby = null!;
	private Node2D? _currentVisualNode;
	private ModelId? _renderedSkinId;
	private int _currentIndex;
	private bool _interactable = true;
	private NTashkentIdleAnimation? _idleAnimation;

	public override void _Ready()
	{
		_leftArrow = GetNode<TextureButton>("VBoxContainer/HBoxContainer/LeftArrow");
		_rightArrow = GetNode<TextureButton>("VBoxContainer/HBoxContainer/RightArrow");
		_visualContainer = GetNode<Control>("VBoxContainer/HBoxContainer/VisualContainer");
		_skinNameLabel = GetNode<Label>("VBoxContainer/LabelContainer/SkinNameLabel");

		_leftArrow.Pressed += OnLeftPressed;
		_rightArrow.Pressed += OnRightPressed;
		VisibilityChanged += ResetIdleAnimation;
	}

	public void SetInteractable(bool interactable)
	{
		_interactable = interactable;
		ResetIdleAnimation();
		if (GodotObject.IsInstanceValid(_leftArrow))
		{
			_leftArrow.Visible = interactable;
			_leftArrow.Disabled = !interactable;
		}

		if (GodotObject.IsInstanceValid(_rightArrow))
		{
			_rightArrow.Visible = interactable;
			_rightArrow.Disabled = !interactable;
		}
	}

	public void ShowAndSync(StartRunLobby lobby, TashkentCharacter selectedSkin)
	{
		_lobby = lobby;
		Visible = true;

		var selectedIndex = Array.FindIndex(Skins, skin => skin.Id == selectedSkin.Id);
		_currentIndex = selectedIndex >= 0 ? selectedIndex : 0;

		RenderSkinVisuals(Skins[_currentIndex]);
	}

	private void OnLeftPressed()
	{
		if (!_interactable || _lobby.LocalPlayer.isReady) return;
		_currentIndex = (_currentIndex + Skins.Length - 1) % Skins.Length;
		ApplySelection();
	}

	private void OnRightPressed()
	{
		if (!_interactable || _lobby.LocalPlayer.isReady) return;
		_currentIndex = (_currentIndex + 1) % Skins.Length;
		ApplySelection();
	}

	private void ApplySelection()
	{
		var skin = Skins[_currentIndex];
		_lobby.SetLocalCharacter(skin, _lobby.LocalPlayer.isRotatingDread);
		TashkentConfig.LastPlayedSkinId = skin.Id.Entry;
		ModConfig.SaveDebounced<TashkentConfig>(0);
		RenderSkinVisuals(skin);
	}

	private void RenderSkinVisuals(TashkentCharacter skin)
	{
		ResetIdleAnimation();
		_skinNameLabel.Text = new LocString("characters", $"{skin.Id.Entry}.skinName").GetFormattedText();

		if (_renderedSkinId == skin.Id && GodotObject.IsInstanceValid(_currentVisualNode))
			return;

		_renderedSkinId = skin.Id;
		if (GodotObject.IsInstanceValid(_currentVisualNode))
		{
			_visualContainer.RemoveChild(_currentVisualNode);
			_currentVisualNode.QueueFree();
			_currentVisualNode = null;
			_idleAnimation = null;
		}

		var scene = ResourceLoader.Load<PackedScene>(skin.CustomVisualPath);
		if (scene is null)
			return;

		_currentVisualNode = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		_visualContainer.AddChild(_currentVisualNode);
		_currentVisualNode.Position = new Vector2(150f, 270f);

		var spineNode = _currentVisualNode.GetNodeOrNull<Node2D>("%Visuals");
		if (!GodotObject.IsInstanceValid(spineNode) || spineNode.GetClass() != "SpineSprite")
			return;

		var sprite = new MegaSprite((Variant)(GodotObject)spineNode);
		_idleAnimation = NTashkentIdleAnimation.AttachPreview(_currentVisualNode, sprite,
			this, () => _interactable);
	}

	private void ResetIdleAnimation()
	{
		if (GodotObject.IsInstanceValid(_idleAnimation))
			_idleAnimation!.ResetActivity();
	}
}
