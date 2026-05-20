using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;
using TashkentSpire2.TashkentSpire2Code.Rewards;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class EternalOathPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/eternaloath_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/eternaloath_power.png";
    
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (this.Owner.Player == null || this.Owner.IsDead) return;
        
        Player? player = room.CombatState.Players.FirstOrDefault(p => p.NetId == this.Owner.Player.NetId);
        
        if (player == null) return;

        for (int i = 0; i < base.Amount; i++)
        {
            room.AddExtraReward(base.Owner.Player, new TashkentOathReward(base.Owner.Player));
        }
    }
}