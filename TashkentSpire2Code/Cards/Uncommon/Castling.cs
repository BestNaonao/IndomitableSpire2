using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class Castling() : TashkentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Retain),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> list1 = PileType.Hand.GetPile(base.Owner).Cards.ToList();
        foreach (CardModel item in list1)
        {
            if (item.Keywords.Contains(CardKeyword.Retain))
            {
                await CardCmd.Exhaust(choiceContext, item);
            }
        }
        
        List<CardModel> list2 = PileType.Hand.GetPile(base.Owner).Cards.ToList();
        foreach (CardModel item in list2)
        {
            item.AddKeyword(CardKeyword.Retain);
        }
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}