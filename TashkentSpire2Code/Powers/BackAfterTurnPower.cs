using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class BackAfterTurnPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/backafterturn_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/backafterturn_power.png";

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            Flash();
            await PowerCmd.Apply<DistancePower>(base.Owner, -base.Amount, base.Owner, null);
            await PowerCmd.Remove(this);
        }
    }
}