using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class ManeuverPower : TashkentPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/core_breakdown_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/core_breakdown_power.png";

    private bool _shouldIgnoreNextInstance;

    public void IgnoreNextInstance() => _shouldIgnoreNextInstance = true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var tips = new List<IHoverTip>();
            tips.Add(HoverTipFactory.FromCard(ModelDb.Card<Maneuver>()));
            tips.Add(HoverTipFactory.FromPower<StrengthPower>());
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
            await PowerCmd.Apply<StrengthPower>(target, -amount, applier, cardSource, silent: true);
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
                await PowerCmd.Apply<StrengthPower>(base.Owner, -amount, applier, cardSource, silent: true);
            }
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            Flash();
            
            await PowerCmd.Remove(this);
            await PowerCmd.Apply<StrengthPower>(base.Owner, base.Amount, base.Owner, null);
        }
    }
}