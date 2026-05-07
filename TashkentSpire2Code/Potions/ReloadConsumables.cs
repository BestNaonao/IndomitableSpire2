using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards;
using TashkentSpire2.TashkentSpire2Code.Commands;

namespace TashkentSpire2.TashkentSpire2Code.Potions;

public sealed class ReloadConsumables : TashkentPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;
     
    public override TargetType TargetType => TargetType.Self;
     
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new LoadDynamicVar(3M)
    ];
     
    public override string CustomPackedImagePath => "res://TashkentSpire2/images/potions/mark_potion.png";
    public override string CustomPackedOutlinePath => "res://TashkentSpire2/images/potions/mark_potion_outline.png";
     
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        CardSelectorPrefs cardSelectorPrefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 1);
        CardModel? card = (await CardSelectCmd.FromHand(
            choiceContext, 
            base.Owner, 
            cardSelectorPrefs, 
            (CardModel c) => c is IAmmunitionCard, 
            this
        )).FirstOrDefault();

        if (card != null)
        {
            await Loadcmd.Execute(choiceContext, card, DynamicVars["TashkentSpire2-Load"].IntValue);
        }
    }
}