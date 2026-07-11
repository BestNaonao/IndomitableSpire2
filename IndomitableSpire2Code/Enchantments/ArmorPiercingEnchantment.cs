using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Enchantments;

public sealed class ArmorPiercingEnchantment : IndomitableEnchantment
{
    // 只能附魔给攻击牌
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;
    
    // 【核心黑魔法】：放弃附魔基类的简易伤害接口，直接拦截最高权限的全局伤害修改钩子！
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay) =>
        // 严格的伤害翻倍触发条件判定：
        // 1. 卡牌来源必须是这张附魔卡
        // 2. 伤害来源必须是打出这张卡的主人
        // 3. 必须是一次基于卡牌数值的攻击（非环境/反伤）
        // 4. 最重要的一点：目标存在，且目标的护甲（Block）大于 0！
        cardSource != Card || dealer != Card.Owner.Creature || !props.IsPoweredAttack() || target is not { Block: > 0 }
            ? 1M : 2M;
}