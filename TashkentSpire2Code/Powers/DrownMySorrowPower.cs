using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards.Token;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public class DrownMySorrowPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/mark_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/mark_power_packed.tres";
    
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (base.Owner.Player == null || card.Owner.Creature != base.Owner || card.Type != CardType.Status)
            return;

        Flash();
        
        CardModel newCard = base.CombatState.CreateCard<Vodka>(base.Owner.Player!);
        
        await CardCmd.Transform(card, newCard);
    }
}