using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Localization.HoverTips;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class NavigatorPower : IndomitablePower, IAfterReconSubscriber
{
    public override PowerType Type => PowerType.Buff;
    
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [CustomHoverTipFactory.FromRecon(), HoverTipFactory.FromPower<MotivationPower>()];
    
    public async Task AfterRecon(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        await PowerCmd.Apply<MotivationPower>(choiceContext, Owner, Amount, Owner, null);
    }
}