namespace IndomitableSpire2.IndomitableSpire2Code.Character;

public sealed class IndomitableCharacter : Indomitable
{
    // 逻辑控制开关：当前选择的皮肤。
    // 设置为 static 方便以后在 UI 界面（如 CharacterSelectScreen）的按钮点击事件中直接修改：
    public override IndomitableSkin CurrentSkin => IndomitableSkin.Default;
    
    // 根据 CurrentSkin 动态获取 CustomVisualPath，指向 Godot 导出的角色视觉场景 (tscn) 包路径
    public override string CustomVisualPath => 
        "res://IndomitableSpire2/scenes/characters/indomitable.tscn";
    // 获取休息点的视觉场景
    public override string CustomRestSiteAnimPath => 
        "res://IndomitableSpire2/scenes/characters/indomitable_rest_site.tscn";
    // 获取商店的视觉场景
    public override string CustomMerchantAnimPath => 
        "res://IndomitableSpire2/scenes/merchant/indomitable_merchant.tscn";
    // 小图标
    public override string CustomIconTexturePath => 
        "res://IndomitableSpire2/images/charui/character_icon_indomitable.png";
    // 角色图标场景
    public override string CustomIconPath => 
        "res://IndomitableSpire2/scenes/characters/indomitable_icon.tscn";
    // 角色图标轮廓
    public override string CustomIconOutlineTexturePath => 
        "res://IndomitableSpire2/images/ui/character_icon_indomitable_outline.png";
    
    // 人物手模图片(石头剪刀布和指向)
    public override string CustomArmPaperTexturePath => 
        "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_paper.png";
    
    public override string CustomArmPointingTexturePath => 
        $"res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_point{new Random().Next(1, 4)}.png";
    
    public override string CustomArmRockTexturePath => 
        "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_rock.png";
    
    public override string CustomArmScissorsTexturePath => 
        "res://IndomitableSpire2/images/charui/hands/multiplayer_hand_indomitable_scissors.png";
}