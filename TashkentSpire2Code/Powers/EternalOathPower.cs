using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public class EternalOathPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterCombatEnd(CombatRoom room)
    {
        for (int i = 0; i < base.Amount; i++)
        {
            room.AddExtraReward(base.Owner.Player!, new CardRemovalReward(base.Owner.Player!));
        }
        return Task.CompletedTask;
    }
}