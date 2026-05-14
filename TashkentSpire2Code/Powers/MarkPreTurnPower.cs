using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class MarkPreTurnPower : TashkentPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MarkPower>()];
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/markpreturn_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/markpreturn_power.png";
    
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Side)
        {
            Flash();
            await PowerCmd.Apply<MarkPower>(base.Owner, base.Amount, base.Owner, null);
        }
    }
}