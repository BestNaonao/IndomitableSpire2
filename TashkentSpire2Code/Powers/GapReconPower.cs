using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class GapReconPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/gaprecon_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/gaprecon_power.png";
    
    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Side)
        {
            return;
        }
        Flash();
        await PowerCmd.Apply<MarkPower>(base.CombatState.HittableEnemies, base.Amount, base.Owner, null);
    }
}