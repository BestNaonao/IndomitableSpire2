using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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
    
    private bool _isDuplicating = false; 

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/mark_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/mark_power.png";
    
    public override int DisplayAmount => base.DynamicVars[RemainKey].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(RemainKey, 0m)
    ];
    
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    { 
        base.DynamicVars[RemainKey].BaseValue = this.Amount;
        Flash();
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }
    
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            base.DynamicVars[RemainKey].BaseValue = this.Amount - _usedThisTurn;
            InvokeDisplayAmountChanged();
        }

        if (_isDuplicating) return;

        if (base.DynamicVars[RemainKey].BaseValue > 0 && 
            cardSource != null && 
            cardSource == _triggeringCard && 
            power.GetTypeForAmount(amount) == PowerType.Buff &&
            power.IsInstanced && 
            power.StackType != PowerStackType.Single)
        {
            _isDuplicating = true;
            
            await PowerCmd.Apply(power, base.Owner, amount, applier, cardSource);
            
            _isDuplicating = false;
        }
    }

    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        _triggeringCard = null;
        _usedThisTurn = 0;
        base.DynamicVars[RemainKey].BaseValue = this.Amount;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        if (_isDuplicating) return Task.CompletedTask;

        if (base.DynamicVars[RemainKey].BaseValue <= 0 || cardSource == null) 
            return Task.CompletedTask;
        
        if (applier != base.Owner.Player?.Creature || target.Side != base.Owner.Player?.Creature.Side) 
            return Task.CompletedTask;
        
        if (power.GetTypeForAmount(amount) != PowerType.Buff) 
            return Task.CompletedTask;
        
        if (power.StackType == PowerStackType.Single) 
            return Task.CompletedTask;
        
        if (_triggeringCard == null)
        {
            _triggeringCard = cardSource;
        }

        return Task.CompletedTask;
    }

    public override decimal ModifyPowerAmountGiven(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (_isDuplicating) return amount;

        if (base.DynamicVars[RemainKey].BaseValue > 0 && 
            cardSource != null && 
            cardSource == _triggeringCard && 
            power.GetTypeForAmount(amount) == PowerType.Buff)
        {
            if (power.StackType == PowerStackType.Single) 
                return amount;
            
            if (power.IsInstanced) 
                return amount;
            
            return amount * 2m;
        }

        return amount;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card == _triggeringCard)
        {
            Flash();
            _triggeringCard = null;
            
            _usedThisTurn++;
            base.DynamicVars[RemainKey].BaseValue = this.Amount - _usedThisTurn;
            
            InvokeDisplayAmountChanged();
        }
        
        await base.AfterCardPlayed(context, cardPlay);
    }
}