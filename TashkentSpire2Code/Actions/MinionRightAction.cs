using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Actions;

public sealed class MinionRightAction : GoneWithTheWindAction
{
    public override string? CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/minion_right.png";
    public override string? CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/minion_right.png";
    
    protected override bool CanExecute(Creature player)
    {
        var distPower = player.GetPower<DistancePower>();
        if (distPower != null && distPower.Amount >= 15m)
        {
            return false;
        }
        return true;
    }
    
    protected override async Task ExecuteEffect(Creature self)
    {
        var player = self.PetOwner?.Creature;
        if (player != null)
        {
            await PowerCmd.Apply<DistancePower>(player, 1m, self, null, false);
        }
    }
}