using BaseLib.Abstracts;
using BaseLib.Patches.UI;
using Godot;

namespace TashkentSpire2.TashkentSpire2Code.Character;

public sealed class TashkentPotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName => CustomEnergyIconPatches.GetEnergyColorName(Id);
    public override Color LabOutlineColor => TashkentCharacter.TopicColor;
    
    public override string BigEnergyIconPath => 
        "res://TashkentSpire2/images/packed/sprite_fonts/Tashkent_energy_icon_original.png";
    public override string TextEnergyIconPath => 
        "res://TashkentSpire2/images/packed/sprite_fonts/Tashkent_energy_icon.png";
}