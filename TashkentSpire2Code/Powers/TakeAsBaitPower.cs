using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class TakeAsBaitPower : TashkentPower
{
    private CardModel? _triggeringCard;
    private bool _hasTriggered = false;
    private bool _usedThisTurn = false;
    private Type? _typeToIgnore;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/TakeAsBaitPower.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/TakeAsBaitPower.png";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (!_usedThisTurn && cardPlay.Player == this.Owner.Player)
        {
            _triggeringCard = cardPlay.Card;
            _hasTriggered = false;
            _typeToIgnore = null;
        }
        else
        {
            _triggeringCard = null;
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (!_usedThisTurn && (amount > 0 || power is DistancePower) && _triggeringCard != null && cardSource == _triggeringCard && cardSource.Owner == this.Owner.Player)
        {
            if (_typeToIgnore != null && power.GetType() == _typeToIgnore)
            {
                _typeToIgnore = null;
                return 0m;
            }

            if (power.GetTypeForAmount(amount) == PowerType.Buff)
            {
                if (power is ITemporaryPower tempPower)
                {
                    _typeToIgnore = tempPower.InternallyAppliedPower?.GetType();
                }

                _hasTriggered = true;
            }
        }
        return 0m;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card == _triggeringCard)
        {
            if (_hasTriggered && !_usedThisTurn)
            {
                Flash();
                _usedThisTurn = true;

                await PlayerCmd.GainEnergy(Amount, Owner.Player!); 
            }

            _triggeringCard = null;
            _hasTriggered = false;
            _typeToIgnore = null;
        }
        await base.AfterCardPlayed(context, cardPlay);
    }

    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        _triggeringCard = null;
        _hasTriggered = false;
        _usedThisTurn = false;
        _typeToIgnore = null;
        return Task.CompletedTask;
    }
}