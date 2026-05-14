using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public abstract class TashkentTemporaryPower<T> : TashkentPower, ITemporaryPower where T : PowerModel
{
    private bool _shouldIgnoreNextInstance;

    public abstract AbstractModel OriginModel { get; }

    public PowerModel InternallyAppliedPower => ModelDb.Power<T>();

    protected virtual bool IsPositive => false; 
    private int Sign => IsPositive ? 1 : -1;

    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerType Type => IsPositive ? PowerType.Buff : PowerType.Debuff;

    public void IgnoreNextInstance() => _shouldIgnoreNextInstance = true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var tips = new List<IHoverTip>();
            if (OriginModel is CardModel card) tips.Add(HoverTipFactory.FromCard(card));

            tips.Add(HoverTipFactory.FromPower<T>());
            return tips;
        }
    }

    public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_shouldIgnoreNextInstance)
        {
            _shouldIgnoreNextInstance = false;
        }
        else
        {
            await PowerCmd.Apply<T>(target, (decimal)Sign * amount, applier, cardSource, silent: true);
        }
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this && amount != (decimal)base.Amount)
        {
            if (_shouldIgnoreNextInstance)
            {
                _shouldIgnoreNextInstance = false;
            }
            else
            {
                await PowerCmd.Apply<T>(base.Owner, (decimal)Sign * amount, applier, cardSource, silent: true);
            }
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            Flash();
            await PowerCmd.Remove(this);
            await PowerCmd.Apply<T>(base.Owner, (decimal)-Sign * base.Amount, base.Owner, null);
        }
    }
}