using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class SpecialServicesPower : TashkentPower
{
    private CardModel? _triggeringCard;
    private bool _hasTriggered = false;
    private Type? _typeToIgnore;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => "res://TashkentSpire2/images/powers/big/SpecialServicesPower.png";
    public override string CustomPackedIconPath => "res://TashkentSpire2/images/powers/packed/SpecialServicesPower.png";

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        _triggeringCard = cardPlay.Card;
        _hasTriggered = false;
        _typeToIgnore = null;
        return Task.CompletedTask;
    }

    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if ((amount > 0 || power is DistancePower) && _triggeringCard != null && cardSource == _triggeringCard && cardSource.Owner == this.Owner.Player)
        {
            if (_typeToIgnore != null && power.GetType() == _typeToIgnore)
            {
                _typeToIgnore = null;
                return 0m;
            }

            if (power.GetTypeForAmount(amount) == PowerType.Buff)
            {
                if (power.StackType == PowerStackType.Single) return amount;

                if (power is ITemporaryPower tempPower)
                {
                    _typeToIgnore = tempPower.InternallyAppliedPower?.GetType();
                }

                _hasTriggered = true; 
                return amount;
            }
        }
        return 0m;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card == _triggeringCard)
        {
            if (_hasTriggered)
            {
                Flash();
                await PowerCmd.Decrement(this);
            }

            _triggeringCard = null;
            _hasTriggered = false;
            _typeToIgnore = null;
        }
        await base.AfterCardPlayed(context, cardPlay);
    }
}