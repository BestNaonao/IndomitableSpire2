using MegaCrit.Sts2.Core.Entities.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class BarrelModificationLoadPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/BarrelModificationLoadPower.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/BarrelModificationLoadPower.png";
}