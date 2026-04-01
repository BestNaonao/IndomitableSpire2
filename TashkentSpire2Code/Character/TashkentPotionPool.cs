using BaseLib.Abstracts;
using Godot;

namespace TashkentSpire2.TashkentSpire2Code.Character;

public sealed class TashkentPotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName => TashkentCharacter.CharacterId;
    public override Color LabOutlineColor => TashkentCharacter.TopicColor;
}