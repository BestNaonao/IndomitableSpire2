using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace IndomitableSpire2.IndomitableSpire2Code.Enchantments;

public sealed class HighExplosiveEnchantment : IndomitableEnchantment
{
    // 是否在卡牌上显示数值
    public override bool ShowAmount => true;
    
    // 是否会添加额外的卡牌描述文本
    public override bool HasExtraCardText => true;
    
    // 默认可以堆叠（允许反复附魔叠加层数）
    public override bool IsStackable => true;
    
    // 限制：只能附魔给可以指定敌人的卡牌（或者至少是攻击牌）
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;
    
    // 悬浮窗提示：显示起火的能力说明
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<OnFirePower>()];
    
    protected override void OnEnchant()
    {
        // 可以在附魔时给卡牌加个 Tag 之类的，如果没有特殊需求可留空，也可以在这里播放特殊音效
    }
    
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        // 只有状态正常且有目标时才触发
        if (Status != EnchantmentStatus.Normal || cardPlay?.Target is not { IsAlive: true }) return;
        
        // 对卡牌的目标施加对应层数的起火
        await PowerCmd.Apply<OnFirePower>(
            target: cardPlay.Target,
            amount: Amount, // 附魔的自带属性，表示层数
            applier: Card.Owner.Creature,
            cardSource: Card
        );
    }
}