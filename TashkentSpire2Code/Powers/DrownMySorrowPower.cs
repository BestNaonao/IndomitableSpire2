using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards.Token;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public class DrownMySorrowPower : TashkentPower
{
    private const string RemainKey = "RemainAmount";

    private int reduce = 0;
    
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/drownmysorrow_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/drownmysorrow_power.png";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Vodka>()];
    
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

    public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (power == this)
        {
            base.DynamicVars[RemainKey].BaseValue = this.Amount - reduce;
            Flash();
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }
    
    public override Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != this.Owner.Player) return Task.CompletedTask;

        base.DynamicVars[RemainKey].BaseValue = this.Amount;
        reduce = 0;
        
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
    
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (base.Owner.Player == null || card.Owner.Creature != base.Owner || card.Type != CardType.Status)
            return;
        if (base.DynamicVars[RemainKey].BaseValue <= 0)
            return;

        reduce++;
        base.DynamicVars[RemainKey].BaseValue = this.Amount - reduce;
        
        Flash();
        
        CardModel newCard = base.CombatState.CreateCard<Vodka>(base.Owner.Player!);
        await CardCmd.Transform(card, newCard);
        
        InvokeDisplayAmountChanged();
    }
}