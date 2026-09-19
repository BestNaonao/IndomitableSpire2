using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class EquipmentUpgrade() : TashkentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8M, ValueProp.Move),
        new LoadDynamicVar(1M)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(TashkentKeyword.Barrage),
        LoadDynamicVar.GetHoverTip()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
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
            card.AddKeyword(TashkentKeyword.Barrage);
            await Loadcmd.Execute(choiceContext, card, DynamicVars["TashkentSpire2-Load"].IntValue);
        }
    }
    
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3M);
}
