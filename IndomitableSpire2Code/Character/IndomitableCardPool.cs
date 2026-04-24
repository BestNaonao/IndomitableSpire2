using BaseLib.Abstracts;
using BaseLib.Patches.UI;
using Godot;

namespace IndomitableSpire2.IndomitableSpire2Code.Character;

public sealed class IndomitableCardPool : CustomCardPoolModel
{
    // 卡池的唯一标识符
    public override string Title => Indomitable.CharacterId;
    // 使用 BaseLib 的标准格式生成 EnergyColorName
    public override string EnergyColorName => CustomEnergyIconPatches.GetEnergyColorName(Id);
    // public override string EnergyColorName => IndomitableCharacter.CharacterId;
    
    // 基础卡牌背景框材质，选择直接硬编码 HSV (色相、饱和度、明度) 的值来微调：
    public override float H => 0.114f;
    public override float S => 0.30f;
    public override float V => 2.25f;
    
    // BaseLib 提供的 ShaderColor，用于对基础卡牌材质进行染色
    public override Color ShaderColor => new("E6F0FA"); // 淡蓝色/纯白色
    public override Color DeckEntryCardColor => new("88BBDD");
    public override Color EnergyOutlineColor => Colors.Black;
    public override bool IsColorless => false;
    
    // UI中使用的大能量图标路径（如能量球显示），和文本中使用的能量图标路径（如卡牌描述）
    public override string BigEnergyIconPath => 
        "res://IndomitableSpire2/images/packed/sprite_fonts/indomitable_energy_icon_original.png";
    public override string TextEnergyIconPath => 
        "res://IndomitableSpire2/images/packed/sprite_fonts/indomitable_energy_icon.png";
}