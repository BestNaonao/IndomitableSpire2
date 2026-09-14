using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class TorpedoGodPower: TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<TorpedoPower>()];
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/TorpedoGodPower.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/TorpedoGodPower.png";

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner != null)
        {
            TorpedoPower.RefreshVisibleTorpedoes(Owner);
        }
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        TorpedoPower.RefreshVisibleTorpedoes(oldOwner);
        return Task.CompletedTask;
    }
}
