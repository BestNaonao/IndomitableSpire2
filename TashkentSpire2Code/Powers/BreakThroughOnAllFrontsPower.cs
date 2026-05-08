using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards;
using TashkentSpire2.TashkentSpire2Code.Commands;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public class BreakThroughOnAllFrontsPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/mark_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/mark_power.png";
    
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if(this.Owner.Player == null)
            return;
        
        var prompt = new LocString("powers", "TASHKENTSPIRE2-BREAK_THROUGH_ON_ALL_FRONTS_POWER.selectionScreenPrompt");
        prompt.Add("amount", (decimal)this.Amount);
        CardSelectorPrefs cardSelectorPrefs = new CardSelectorPrefs(prompt, 1);
        
        CardModel? loadcard = (await CardSelectCmd.FromHand(
            choiceContext, 
            this.Owner.Player, 
            cardSelectorPrefs, 
            (CardModel c) => c is IAmmunitionCard, 
            this
        )).FirstOrDefault();

        if (loadcard != null)
        {
            await Loadcmd.Execute(choiceContext, loadcard, this.Amount);
        }
    }
}