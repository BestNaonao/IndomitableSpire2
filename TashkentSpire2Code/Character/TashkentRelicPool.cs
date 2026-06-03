using BaseLib.Abstracts;
using Godot;
using BaseLib.Patches.UI;

namespace TashkentSpire2.TashkentSpire2Code.Character;

public sealed class TashkentRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => CustomEnergyIconPatches.GetEnergyColorName(Id);
    public override Color LabOutlineColor => TashkentCharacter.TopicColor;
    
    public override string BigEnergyIconPath => 
        "res://TashkentSpire2/images/packed/sprite_fonts/Tashkent_energy_icon_original.png";
    public override string TextEnergyIconPath => 
        "res://TashkentSpire2/images/packed/sprite_fonts/Tashkent_energy_icon.png";
}