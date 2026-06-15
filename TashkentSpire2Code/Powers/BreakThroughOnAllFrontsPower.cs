using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Enchantment;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public class BreakThroughOnAllFrontsPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        ..HoverTipFactory.FromEnchantment<EndeavourEnchantment>()
    ];
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/breakthroughonallfronts_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/breakthroughonallfronts_power.png";
    
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if(this.Owner.Player == null || player != this.Owner.Player)
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
            
            CardModel copy = loadcard.CreateClone();
            if (ModelDb.Enchantment<EndeavourEnchantment>().CanEnchant(copy))
            {
                CardCmd.Enchant<EndeavourEnchantment>(copy, 1m);
            }
            
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, base.Owner.Player);
        }
    }
}