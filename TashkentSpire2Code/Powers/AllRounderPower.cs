using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class AllRounderPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/AllRounderPower.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/AllRounderPower.png";
    
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}