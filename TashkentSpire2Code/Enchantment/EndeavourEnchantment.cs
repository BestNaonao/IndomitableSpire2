using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;

namespace TashkentSpire2.TashkentSpire2Code.Enchantment;

public sealed class EndeavourEnchantment : CustomEnchantmentModel
{
    public override bool ShowAmount => false;

    public override bool HasExtraCardText => true;

    protected override string? CustomIconPath =>
        "res://TashkentSpire2/images/enchantment/ferment_enchantment.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];
    
    protected override void OnEnchant()
    {
        Card.AddKeyword(CardKeyword.Exhaust);
        base.Card.SetToFreeThisCombat();
    }
    
    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack || cardType == CardType.Skill;
    }
}