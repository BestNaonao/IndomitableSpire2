using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class InternationalOrdersPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 参考王国资产，战斗结束后发放金币奖励
    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.Player is { } player) room.AddExtraReward(player, new GoldReward(Amount, player));
        return Task.CompletedTask;
    }
}