using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

[GlobalClass]
public partial class SkinSelectPanel : Control
{
    private TextureButton _leftArrow = null!;
    private TextureButton _rightArrow = null!;
    private Control _visualContainer = null!;   // 替换为占位的 Control 容器
    private MegaLabel _skinNameLabel = null!;
    private StartRunLobby _lobby = null!;
    private NCharacterSelectButton _button = null!;
    
    private Node2D? _currentVisualNode;  // 记录当前实例化的模型节点，用于在切换时销毁
    private int _currentIndex;
    private ModelId? _renderedSkinId;
    
    private static readonly IndomitableSkin[] Skins = Enum.GetValues<IndomitableSkin>();
    
    public override void _Ready()
    {
        _leftArrow = GetNode<TextureButton>("VBoxContainer/HBoxContainer/LeftArrow");
        _rightArrow = GetNode<TextureButton>("VBoxContainer/HBoxContainer/RightArrow");
        _visualContainer = GetNode<Control>("VBoxContainer/HBoxContainer/VisualContainer");
        _skinNameLabel = GetNode<MegaLabel>("VBoxContainer/LabelContainer/SkinNameLabel");
        
        _leftArrow.Pressed += OnLeftPressed;
        _rightArrow.Pressed += OnRightPressed;
        VisibilityChanged += SyncPreviewProcessing; // 隐藏时取消可处理性，可见时继承可处理性，以节省性能。
        SyncPreviewProcessing();
    }
    
    private void SyncPreviewProcessing() =>
        _visualContainer.ProcessMode = IsVisibleInTree() ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
    
    private void SetInteractable(bool interactable)
    {
        // 直接控制左和右箭头的可见性（或者设置 Disabled 属性也可以）
        if (IsInstanceValid(_leftArrow))
        {
            _leftArrow.Visible = interactable;
            _leftArrow.Disabled = !interactable;
        }
        if (IsInstanceValid(_rightArrow))
        {
            _rightArrow.Visible = interactable;
            _rightArrow.Disabled = !interactable;
        }
    }
    
    // 每次选中该角色时被调用（包括重进界面）
    public void ShowSelection(StartRunLobby lobby, NCharacterSelectButton button, Indomitable skin)
    {
        _lobby = lobby;
        _button = button;
        _currentIndex = Array.IndexOf(Skins, skin.CurrentSkin);
        SetInteractable(button.IsEnabled);
        RenderSkinVisuals(skin);
        Show(); // 先准备再渲染，确保视觉平滑过渡
    }
    
    // 每帧同步回调，与按钮状态不同时同步状态
    public override void _Process(double delta)
    {
        if (IsVisibleInTree() && IsInstanceValid(_button) && _leftArrow.Disabled == _button.IsEnabled)
            SetInteractable(_button.IsEnabled);
    }
    
    private void OnLeftPressed() => ChangeSkin(-1);
    
    private void OnRightPressed() => ChangeSkin(1);
    
    private void ChangeSkin(int direction)
    {
        if (!IsVisibleInTree() || !_button.IsEnabled) return;
        _currentIndex = (_currentIndex + direction + Skins.Length) % Skins.Length;
        var skin = SkinPreference.Model(Skins[_currentIndex]);
        SkinPreference.Remember(skin.CurrentSkin);
        SkinLobbyHelper.RefreshSkinButtons(_button.GetParent(), skin);
        _lobby.SetLocalCharacter(skin, _lobby.LocalPlayer.isRotatingDread);
        RenderSkinVisuals(skin);
        // 播放当前皮肤的专属选人音效
        SfxCmd.Play(skin.CharacterSelectSfx);
    }
    
    // 专注处理 UI 视觉的替换
    private void RenderSkinVisuals(Indomitable skin)
    {
        _skinNameLabel.SetTextAutoSize(new LocString("characters", $"{skin.Id.Entry}.skinName").GetFormattedText());
        PreparePreview(skin);
    }
    
    // 可在面板隐藏时提前装入模型。正式显示时按当前皮肤核对并复用同一实例。
    internal void PreparePreview(Indomitable skin)
    {
        if (_renderedSkinId == skin.Id && IsInstanceValid(_currentVisualNode))
            return;
        
        _renderedSkinId = skin.Id;
        // 1. 清理上一套皮肤的节点
        if (IsInstanceValid(_currentVisualNode))
        {
            _visualContainer.RemoveChild(_currentVisualNode);
            _currentVisualNode.QueueFree();
            _currentVisualNode = null;
        }
        
        // 2. 动态加载并实例化 CustomVisualPath 对应的场景
        var scene = ResourceLoader.Load<PackedScene>(skin.CustomVisualPath);
        if (scene == null) return;
        _currentVisualNode = scene.Instantiate<Node2D>();
        _visualContainer.AddChild(_currentVisualNode);
        
        // 【核心修复】：将 Node2D 的位置定在 VisualContainer 的中下方
        // 容器宽度是 300，高度是 400。X=150 是水平居中，Y=250 是将脚底放在偏下部。
        _currentVisualNode.Position = new Vector2(150, 250);
        
        // 尝试获取模型内的 SpineSprite 并播放待机动画
        var spineNode = _currentVisualNode.GetNodeOrNull<Node>("%Visuals");
        if (spineNode == null) return;
        var megaSprite = new MegaSprite((Variant)(GodotObject)spineNode);
        megaSprite.GetAnimationState().SetAnimation("normal");
        IndomitableIdleAnimation.AttachPreview(_currentVisualNode, megaSprite);
    }
}