using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class DrowsyPower : IndomitablePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<HypnotizedPower>()];
    
    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        if (card.Owner.Creature != Owner || amount <= 0) return;
        // 每次支付只触发一次，X 费也按一次计算；困倦层数决定每次获得的催眠层数。
        Flash();
        await PowerCmd.Apply<HypnotizedPower>(new ThrowingPlayerChoiceContext(), Owner, Amount, Owner, card);
    }
    
    public override async Task AfterSideTurnEndLate(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side && participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}