using BaseLib.Abstracts;
using Godot;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Character;

public sealed class TashkentRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => TashkentCharacter.CharacterId;
    public override Color LabOutlineColor => TashkentCharacter.TopicColor;
}