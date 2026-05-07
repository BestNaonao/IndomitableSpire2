using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace TashkentSpire2.TashkentSpire2Code.Enchantment;

public sealed class OathEnchantment : CustomEnchantmentModel
{
    public override bool ShowAmount => false;   // 是否在卡牌上显示数值

    // 重载这个以改变显示的数字
    // public override int DisplayAmount => DynamicVars.Cards.IntValue;
    
    public override bool HasExtraCardText => true;  // 是否会添加额外的卡牌描述文本

    // 像卡牌、遗物、药水等一样，可以使用DynamicVars和ExtraHoverTips
    //protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Eternal),
        HoverTipFactory.FromKeyword(CardKeyword.Retain)
    ];
    
    protected override string? CustomIconPath => "res://TashkentSpire2/images/enchantment/oath_enchantment.png";

    // public override bool CanEnchant(CardModel card) // 决定是否可以附魔到某张卡牌上
    // { }
    
    protected override void OnEnchant() // 当附魔被应用时调用
    {
        Card.AddKeyword(CardKeyword.Eternal);
        Card.AddKeyword(CardKeyword.Retain);
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player == Card.Owner && CombatManager.Instance.History.CardPlaysFinished.Any((CardPlayFinishedEntry e) => e.RoundNumber == combatState.RoundNumber - 1 && e.CardPlay.Card == Card))
        {
            CardPile? pile = Card.Pile;
            if (pile == null || pile.Type != PileType.Hand)
            {
                await CardPileCmd.Add(Card, PileType.Hand);
            }
        }
    }
    // public override decimal EnchantBlockAdditive(decimal originalBlock, ValueProp props) // 修改卡牌获得的值，返回增加的改变量。
    // {
    //     return Amount;
    // }
    
    // public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)    // 当附魔的卡牌被打出时调用。
    // {
    //     Status = EnchantmentStatus.Disabled;    // 打出后可设置为Disabled。
    // }
}