using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

[GlobalClass]
public partial class SkinSelectPanel : Control
{
    private TextureButton _leftArrow = null!;
    private TextureButton _rightArrow = null!;
    private Control _visualContainer = null!;   // 替换为占位的 Control 容器
    private MegaLabel _skinNameLabel = null!;
    private NCharacterSelectScreen _selectScreen = null!;
    
    private Node2D? _currentVisualNode;  // 记录当前实例化的模型节点，用于在切换时销毁
    private int _currentIndex;
    private ModelId? _renderedSkinId;
    
    // 直接使用角色模型列表
    private static readonly List<Indomitable> Skins = 
    [
        ModelDb.Character<IndomitableDefault>(),
        ModelDb.Character<IndomitableMaid>(),
        ModelDb.Character<IndomitableRaceQueen>()
    ];
    
    public override void _Ready()
    {
        _leftArrow = GetNode<TextureButton>("VBoxContainer/HBoxContainer/LeftArrow");
        _rightArrow = GetNode<TextureButton>("VBoxContainer/HBoxContainer/RightArrow");
        _visualContainer = GetNode<Control>("VBoxContainer/HBoxContainer/VisualContainer");
        _skinNameLabel = GetNode<MegaLabel>("VBoxContainer/LabelContainer/SkinNameLabel");
        
        _leftArrow.Pressed += OnLeftPressed;
        _rightArrow.Pressed += OnRightPressed;
    }
    
    public void SetInteractable(bool interactable)
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
    public void ShowAndSync(NCharacterSelectScreen screen, Indomitable selectedSkin)
    {
        _selectScreen = screen; // 及时更新为当前最新的角色选择屏幕（大厅对象可能已重建）
        Visible = true;
        var selectedIndex = Skins.FindIndex(skin => skin.Id == selectedSkin.Id);
        _currentIndex = selectedIndex >= 0 ? selectedIndex : 0;
        
        // SelectCharacter 已经把选择的角色写入大厅。
        RenderSkinVisuals(Skins[_currentIndex]);
    }
    
    private void OnLeftPressed()
    {
        _currentIndex = (_currentIndex + Skins.Count - 1) % Skins.Count;
        OnSelection();
    }
    
    private void OnRightPressed()
    {
        _currentIndex = (_currentIndex + 1) % Skins.Count;
        OnSelection();
    }
    
    private void OnSelection()
    {
        var skin = Skins[_currentIndex];
        _selectScreen.Lobby.SetLocalCharacter(skin);
        RenderSkinVisuals(skin);
        
        // 播放当前皮肤的专属选人音效
        SfxCmd.Play(skin.CharacterSelectSfx);
    }
    
    // 专注处理 UI 视觉的替换
    private void RenderSkinVisuals(Indomitable skin)
    {
        _skinNameLabel.SetTextAutoSize(new LocString("characters", $"{skin.Id.Entry}.skinName").GetFormattedText());
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
        if (scene != null)
        {
            _currentVisualNode = scene.Instantiate<Node2D>();
            _visualContainer.AddChild(_currentVisualNode);
            
            // 【核心修复】：将 Node2D 的位置定在 VisualContainer 的中下方
            // 容器宽度是 300，高度是 400。X=150 是水平居中，Y=250 是将脚底放在偏下部。
            _currentVisualNode.Position = new Vector2(150, 250);
            
            // 尝试获取模型内的 SpineSprite 并播放待机动画
            var spineNode = _currentVisualNode.GetNodeOrNull<Node>("%Visuals");
            if (spineNode != null)
            {
                var megaSprite = new MegaSprite((Variant)(GodotObject)spineNode);
                megaSprite.GetAnimationState().SetAnimation("normal");
            }
        }
    }
}