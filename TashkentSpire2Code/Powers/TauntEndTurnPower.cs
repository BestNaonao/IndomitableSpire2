using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class TauntEndTurnPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/taunt_end_turn_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/taunt_end_turn_power.png";
    
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(base.Owner))
            return;
        
        int distanceAmount = Owner?.GetPower<DistancePower>()?.Amount ?? 0;
        if (distanceAmount != 0 && base.Owner != null)
        {
            await PowerCmd.Apply<DistancePower>(choiceContext, base.Owner, -distanceAmount, base.Owner, null);
        }
        await PowerCmd.Remove(this);
    }
}