using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class MarkPreTurnPower : TashkentPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MarkPower>(),
        HoverTipFactory.FromCard<TakeAsBait>()
    ];
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/markpreturn_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/markpreturn_power.png";
    
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(base.Owner))
        {
            Flash();
            await PowerCmd.Apply<MarkPower>(new ThrowingPlayerChoiceContext(), base.Owner, base.Amount, base.Owner, null);
        }
    }
}