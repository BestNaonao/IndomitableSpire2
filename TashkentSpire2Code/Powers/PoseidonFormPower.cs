using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class PoseidonFormPower : TashkentPower
{
    private const string RemainKey = "RemainAmount";
    private CardModel? _triggeringCard;
    private int _usedThisTurn = 0;
    private bool _hasTriggered = false;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/poseidon_form_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/poseidon_form_power.png";
    
    public override int DisplayAmount => (int)this.Amount - _usedThisTurn;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(RemainKey, 0m)];

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (DisplayAmount > 0)
        {
            _triggeringCard = cardPlay.Card;
            _hasTriggered = false;
        }
        else
        {
            _triggeringCard = null;
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyPowerAmountGiven(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (DisplayAmount > 0 && amount > 0 && _triggeringCard != null && 
            cardSource == _triggeringCard && power.GetTypeForAmount(amount) == PowerType.Buff)
        {
            if (power.StackType == PowerStackType.Single) return amount;

            _hasTriggered = true; 
            return amount * 2m;
        }
        return amount;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card == _triggeringCard)
        {
            if (_hasTriggered)
            {
                Flash();
                _usedThisTurn++;

                InvokeDisplayAmountChanged();
            }

            _triggeringCard = null;
            _hasTriggered = false;
        }
        await base.AfterCardPlayed(context, cardPlay);
    }

    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        _triggeringCard = null;
        _hasTriggered = false;
        _usedThisTurn = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}