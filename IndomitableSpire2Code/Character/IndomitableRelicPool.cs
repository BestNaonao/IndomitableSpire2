using BaseLib.Abstracts;
using Godot;

namespace IndomitableSpire2.IndomitableSpire2Code.Character;

public sealed class IndomitableRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => IndomitableCharacter.CharacterId;
    public override Color LabOutlineColor => IndomitableCharacter.TopicColor;
}