using MegaCrit.Sts2.Core.Entities.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class OxygenTorpedoPower: TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/oxygentorpedo_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/oxygentorpedo_power.png";
}