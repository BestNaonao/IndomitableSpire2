using BaseLib.Abstracts;
using BaseLib.Patches.UI;
using Godot;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Character;

public sealed class TashkentCardPool : CustomCardPoolModel, ICustomEnergyIconPool
{
    // 卡池的唯一标识符
    public override string Title => TashkentCharacter.CharacterId;
    // 使用 BaseLib 的标准格式生成 EnergyColorName
    public override string EnergyColorName => CustomEnergyIconPatches.GetEnergyColorName(Id);
    // public override string EnergyColorName => IndomitableCharacter.CharacterId;

    // 基础卡牌背景框材质，如果没有自定义材质，可以使用游戏原版的
    public override string CardFrameMaterialPath => "card_frame_blue"; 
    
    // BaseLib 提供的 ShaderColor，用于对基础卡牌材质进行染色
    public override Color ShaderColor => new("E6F0FA"); // 淡蓝色/纯白色
    public override Color DeckEntryCardColor => new("88BBDD");
    public override Color EnergyOutlineColor => Colors.Black;
    public override bool IsColorless => false;
    
    // UI中使用的大能量图标路径（如能量球显示），和文本中使用的能量图标路径（如卡牌描述）
    public override string BigEnergyIconPath => 
        "res://TashkentSpire2/images/packed/sprite_fonts/tashkent_energy_icon_original.png";
    public override string TextEnergyIconPath => 
        "res://TashkentSpire2/images/packed/sprite_fonts/tashkent_energy_icon.png";
    
    // 未来可能需要重写：
    // protected override IEnumerable<CardModel> FilterThroughEpochs
}