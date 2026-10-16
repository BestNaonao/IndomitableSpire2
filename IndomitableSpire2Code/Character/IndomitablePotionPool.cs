using BaseLib.Abstracts;
using BaseLib.Patches.UI;
using Godot;

namespace IndomitableSpire2.IndomitableSpire2Code.Character;

public sealed class IndomitablePotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName => CustomEnergyIconPatches.GetEnergyColorName(Id);
    public override Color LabOutlineColor => Indomitable.TopicColor;
    
    public override string BigEnergyIconPath => 
        "res://IndomitableSpire2/images/packed/sprite_fonts/indomitable_energy_icon_original.png";
    public override string TextEnergyIconPath => 
        "res://IndomitableSpire2/images/packed/sprite_fonts/indomitable_energy_icon.png";
}