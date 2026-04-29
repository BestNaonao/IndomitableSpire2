using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

[GlobalClass]
public partial class SkinSelectPanel : Control
{
    private TextureButton _leftArrow;
    private TextureButton _rightArrow;
    private Control _visualContainer;   // 替换为占位的 Control 容器
    private Node2D _currentVisualNode;  // 记录当前实例化的模型节点，用于在切换时销毁
    private MegaLabel _skinNameLabel;

    private NCharacterSelectScreen _selectScreen;
    private int _currentIndex;
    
    // 直接使用角色模型列表
    private List<Indomitable> _skins;
    
    public override void _Ready()
    {
        _leftArrow = GetNode<TextureButton>("HBoxContainer/LeftArrow");
        _rightArrow = GetNode<TextureButton>("HBoxContainer/RightArrow");
        _visualContainer = GetNode<Control>("HBoxContainer/VisualContainer");
        _skinNameLabel = GetNode<MegaLabel>("HBoxContainer/SkinNameLabel");

        _leftArrow.Pressed += OnLeftPressed;
        _rightArrow.Pressed += OnRightPressed;
    }

    public void Initialize(NCharacterSelectScreen screen, CharacterModel currentCharacter)
    {
        _selectScreen = screen;
        
        // 直接从 ModelDb 获取继承了基类的角色实例
        _skins =
        [
            ModelDb.Character<IndomitableCharacter>(),
            ModelDb.Character<IndomitableMaidCharacter>()
        ];

        _currentIndex = _skins.FindIndex(s => s == currentCharacter);
        if (_currentIndex < 0) _currentIndex = 0;

        UpdateUI();
    }

    private void OnLeftPressed()
    {
        _currentIndex = (_currentIndex + _skins.Count - 1) % _skins.Count;
        UpdateUI();
    }

    private void OnRightPressed()
    {
        _currentIndex = (_currentIndex + 1) % _skins.Count;
        UpdateUI();
    }

    private void UpdateUI()
    {
        var skin = _skins[_currentIndex];

        // 1. 清理上一套皮肤的节点
        if (IsInstanceValid(_currentVisualNode))
        {
            _visualContainer.RemoveChild(_currentVisualNode);
            _currentVisualNode.QueueFree();
        }

        // 2. 动态加载并实例化 CustomVisualPath 对应的场景
        var scene = ResourceLoader.Load<PackedScene>(skin.CustomVisualPath);
        if (scene != null)
        {
            _currentVisualNode = scene.Instantiate<Node2D>();
            _visualContainer.AddChild(_currentVisualNode);
            
            // 将 Node2D 相对 Control 容器居中并向下移动（因为角色的原点通常在脚底）
            _currentVisualNode.Position = Vector2.Zero; 
            
            // 尝试获取模型内的 SpineSprite 并播放待机动画
            // 你的模型场景中设置了 unique_name_in_owner = true，可以直接用 "%Visuals" 查找
            var spineNode = _currentVisualNode.GetNodeOrNull<Node>("%Visuals");
            if (spineNode != null)
            {
                // 将 Godot 的节点强制包装为 MegaCrit 提供的 C++ 桥接精灵
                var megaSprite = new MegaSprite((Variant)(GodotObject)spineNode);
                
                // 播放名为 "normal" 的动画，true 表示循环，轨道 0
                megaSprite.GetAnimationState().SetAnimation("normal");
            }
        }

        // 3. 获取多语言软编码文本，并进行网络同步
        _skinNameLabel.SetTextAutoSize(new LocString("characters", $"{skin.Id.Entry}.title").GetFormattedText());
        _selectScreen.Lobby.SetLocalCharacter(skin);
        
        // 4. 播放当前皮肤的专属选人音效
        // SfxCmd.Play(skin.CharacterSelectSfx);
    }
}
