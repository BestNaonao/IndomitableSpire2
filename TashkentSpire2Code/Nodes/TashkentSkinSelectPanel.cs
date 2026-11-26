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

	internal void SetScreenLayout(Vector2 anchor, Vector2 centerOffset = default, float scale = 1f,
		Vector2? contentSize = null)
	{
		var size = contentSize ?? new Vector2(700f, 360f);
		var content = GetNode<Control>("VBoxContainer");
		content.AnchorLeft = content.AnchorRight = anchor.X;
		content.AnchorTop = content.AnchorBottom = anchor.Y;
		content.OffsetLeft = centerOffset.X - size.X * scale / 2f;
		content.OffsetTop = centerOffset.Y - size.Y * scale / 2f;
		content.OffsetRight = content.OffsetLeft + size.X;
		content.OffsetBottom = content.OffsetTop + size.Y;
		content.Scale = Vector2.One * scale;
	}

	internal void PlaceAboveSeed(Control seedInput)
	{
		const float scale = 0.8f;
		SetNarrowContent(false);
		GetNode<Control>("VBoxContainer").Scale = Vector2.One * scale;
		var labelContainer = GetNode<MarginContainer>("VBoxContainer/LabelContainer");
		labelContainer.AddThemeConstantOverride("margin_left", -20);
		labelContainer.AddThemeConstantOverride("margin_right", -20);
		labelContainer.AddThemeConstantOverride("margin_top", -10);
		labelContainer.AddThemeConstantOverride("margin_bottom", -10);
		_skinNameLabel.CustomMinimumSize = new Vector2(420f, 100f);
		_skinNameLabel.AutowrapMode = TextServer.AutowrapMode.Off;
		_skinNameLabel.MaxLinesVisible = -1;
		_skinNameLabel.ClipText = false;
		var layout = GetNode<VBoxContainer>("VBoxContainer");
		layout.Size = layout.GetCombinedMinimumSize();
		var size = layout.Size * scale;
		layout.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop, LayoutPresetMode.KeepSize);
		layout.GrowVertical = GrowDirection.Begin;
		layout.Position = new Vector2((Size.X - size.X) / 2f + 12f,
			seedInput.GlobalPosition.Y - GlobalPosition.Y - size.Y - 20f);
	}

	internal void SetCompactScreenLayout(Vector2 anchor, Vector2 centerOffset, bool narrow = false)
	{
		SetNarrowContent(narrow);
		var labelContainer = GetNode<MarginContainer>("VBoxContainer/LabelContainer");
		foreach (string edge in new[] { "left", "top", "right", "bottom" })
			labelContainer.AddThemeConstantOverride("margin_" + edge, 0);
		_skinNameLabel.CustomMinimumSize = new Vector2(narrow ? 420f : 660f, 128f);
		_skinNameLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_skinNameLabel.MaxLinesVisible = 2;
		_skinNameLabel.ClipText = true;
		SetScreenLayout(anchor, centerOffset, 0.5f, new Vector2(narrow ? 440f : 700f, 432f));
	}

	private void SetNarrowContent(bool narrow)
	{
		GetNode<Control>("VBoxContainer").CustomMinimumSize = new Vector2(narrow ? 440f : 700f, 360f);
		_skinNameLabel.AddThemeFontSizeOverride("font_size", narrow ? 36 : 42);
		foreach (var arrow in new[] { _leftArrow, _rightArrow })
		{
			arrow.IgnoreTextureSize = narrow;
			arrow.CustomMinimumSize = narrow ? new Vector2(64f, 64f) : new Vector2(128f, 0f);
			arrow.StretchMode = narrow ? TextureButton.StretchModeEnum.KeepAspectCentered : TextureButton.StretchModeEnum.KeepCentered;
		}
	}

	private void OnLeftPressed()
	{
		if (!_interactable || !IsVisibleInTree() || _lobby.LocalPlayer.isReady
			|| _lobby.LocalPlayer.character is not TashkentCharacter) return;
		_currentIndex = (_currentIndex + Skins.Length - 1) % Skins.Length;
		ApplySelection();
	}

	private void OnRightPressed()
	{
		if (!_interactable || !IsVisibleInTree() || _lobby.LocalPlayer.isReady
			|| _lobby.LocalPlayer.character is not TashkentCharacter) return;
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
