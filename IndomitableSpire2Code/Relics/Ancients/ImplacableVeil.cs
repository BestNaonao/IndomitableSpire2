using IndomitableSpire2.IndomitableSpire2Code.Cards.Ancients;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics.Ancients;

public sealed class ImplacableVeil : ImplacableRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    
    public override bool HasUponPickupEffect => true;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<CreedOfReprisal>();
    
    public override async Task AfterObtained()
    {
        // 参考原版 珠宝盒 / 寻龙尺：入组经过正常钩子，并显示获得卡牌的预览动画。
        var card = Owner.RunState.CreateCard<CreedOfReprisal>(Owner);
        CardCmd.PreviewCardPileAdd([await CardPileCmd.Add(card, PileType.Deck)], 2f);
    }
}