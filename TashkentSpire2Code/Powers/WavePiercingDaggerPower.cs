using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class WavePiercingDaggerPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/WavePiercingDaggerPower.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/WavePiercingDaggerPower.png";
    
    public override async Task AfterAttack(AttackCommand command)
    {
        if (command.Attacker != base.Owner || command.TargetSide == base.Owner.Side || !command.DamageProps.IsPoweredAttack())
        {
            return;
        }
        
        int totalDamage = command.Results.Sum(r => r.TotalDamage + r.OverkillDamage);

        if (totalDamage > 0)
        {
            await CreatureCmd.GainBlock(base.Owner, totalDamage, ValueProp.Move, null);
        }
        
        await PowerCmd.Decrement(this);
    }
}