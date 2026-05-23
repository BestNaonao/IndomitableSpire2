using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class MarkPower : TashkentPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/mark_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/mark_power.png";
    
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner || !props.HasFlag(ValueProp.Move))
            return 0m;
        
        int counterattackAmount = (int)(base.Owner?.GetPower<CounterattackPower>()?.Amount ?? 0m);
        if (counterattackAmount > 0)
        {
            return -base.Amount;
        }
        
        return base.Amount;
    }
    
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == base.Owner && result.TotalDamage != 0 && props.HasFlag(ValueProp.Move))
        {
            Flash();
            await PowerCmd.Decrement(this);
        }
    }
    
    public override async Task AfterEnergyReset(Player player)
    {
        int counterattackAmount = (int)(base.Owner?.GetPower<CounterattackPower>()?.Amount ?? 0m);
        if (counterattackAmount > 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}