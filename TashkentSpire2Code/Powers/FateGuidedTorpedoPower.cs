using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class FateGuidedTorpedoPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/mark_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/mark_power.png";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<TorpedoPower>()];
    
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Side)
        {
            for (int i = 0; i < base.Amount; i++)
            {
                await PowerCmd.Apply<TorpedoPower>(base.Owner, DynamicVars["TashkentSpire2-Torpedo"].BaseValue, base.Owner, null);
            }
        }
    }
}