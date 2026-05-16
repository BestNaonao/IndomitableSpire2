using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class GoneWithTheWindPower: TashkentPower
{
private const string RemainKey = "Tashkent_GoneRemain";
    private int _reduce = 0;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => "res://TashkentSpire2/images/powers/big/GoneWithTheWindPower.png";
    public override string CustomPackedIconPath => "res://TashkentSpire2/images/powers/packed/GoneWithTheWindPower.png";
    
    public override int DisplayAmount => base.DynamicVars[RemainKey].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(RemainKey, 0m)];

    private void SyncRemainAmount()
    {
        base.DynamicVars[RemainKey].BaseValue = Math.Max(0, this.Amount - _reduce);
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncRemainAmount();
        await Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            SyncRemainAmount();
            Flash();
        }
        return Task.CompletedTask;
    }
    
    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != this.Owner.Player) return Task.CompletedTask;
        
        _reduce = 0;
        SyncRemainAmount();
        return Task.CompletedTask;
    }
    
    public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Side)
            return Task.CompletedTask;
        
        _reduce = (int)this.Amount;
        SyncRemainAmount();
        return Task.CompletedTask;
    }
    
    public async Task<bool> TryConsumeCharge()
    {
        await _lock.WaitAsync();
        try
        {
            if (this.Amount - _reduce > 0)
            {
                _reduce++;
                SyncRemainAmount();
                Flash();
                return true;
            }
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }
}