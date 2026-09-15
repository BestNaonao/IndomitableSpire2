using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class PoseidonFormPower : TashkentPower
{
    private const string RemainKey = "RemainAmount";
    private CardModel? _triggeringCard;
    private int _usedThisTurn = 0;
    private bool _hasTriggered = false;

    private Type? _typeToIgnore;
    private NPoseidonFormVfx? _vfx;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => "res://TashkentSpire2/images/powers/big/poseidon_form_power.png";
    public override string CustomPackedIconPath => "res://TashkentSpire2/images/powers/packed/poseidon_form_power.png";
    
    public override int DisplayAmount => (int)this.Amount - _usedThisTurn;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(RemainKey, 0m)];

    private NPoseidonFormVfx? Vfx
    {
        get => _vfx != null && _vfx.IsValid() ? _vfx : null;
        set
        {
            AssertMutable();
            _vfx = value;
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Vfx = NPoseidonFormVfx.Create(Owner);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        Vfx?.SetActive(false);
        return Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (DisplayAmount > 0)
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
        if (DisplayAmount > 0 && (amount > 0 || power is DistancePower) && _triggeringCard != null && cardSource == _triggeringCard && cardSource.Owner == this.Owner.Player)
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
                Vfx?.OnEffectTriggered();
                _usedThisTurn++;
                InvokeDisplayAmountChanged();
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
        _usedThisTurn = 0;
        _typeToIgnore = null;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
