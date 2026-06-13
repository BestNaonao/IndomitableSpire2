using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards.Ancient;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class KGB : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/KGB.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/KGB.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/KGB.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<EspionageWarfare>(true);

    public override async Task AfterObtained()
    {
        CardModel card = base.Owner.RunState.CreateCard<EspionageWarfare>(base.Owner);
        CardCmd.Upgrade(card);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }
}