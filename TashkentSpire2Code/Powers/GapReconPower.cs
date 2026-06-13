using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class GapReconPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MarkPower>()];
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/gaprecon_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/gaprecon_power.png";
    
    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(base.Owner))
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<MarkPower>(choiceContext, base.CombatState.HittableEnemies, base.Amount, base.Owner, null);
    }
}