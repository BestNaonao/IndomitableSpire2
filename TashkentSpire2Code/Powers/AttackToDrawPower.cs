using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class AttackToDrawPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/mark_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/mark_power.png";
    
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner.Player || cardPlay.Card.Type != CardType.Attack || base.Owner.Player == null)
        {
            return;
        }
        
        await CardPileCmd.Draw(context, 1, base.Owner.Player);
        
        await PowerCmd.Decrement(this);
    }
}