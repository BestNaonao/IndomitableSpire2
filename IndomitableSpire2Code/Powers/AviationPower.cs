using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public class AviationPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 允许为负数
    public override bool AllowNegative => true;
    
    // 使用明亮的金黄色作为航空Buff的标志色
    public override Color AmountLabelColor => new("FFD800");
    
    // 统一将每层加成固定为 1%
    private const decimal MultiplierPerStack = 0.01m;
    
    /// <summary>
    /// 判断来源卡牌是否能获得此航空能力的加成。基类接受任意航空 Tag 或关键词。
    /// </summary>
    protected virtual bool IsAffectedCard(CardModel card) => card.IsCarrierAircraft();
    
    // 拦截伤害与格挡的乘区计算
    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) 
        // 确保攻击者是自己，伤害属于正常受力量加成的攻击，且来源卡牌符合航空分类
        => dealer == Owner && cardSource != null && props.IsPoweredAttack() && IsAffectedCard(cardSource)
            ? 1m + Amount * MultiplierPerStack : 1m;
    
    public override decimal ModifyBlockMultiplicative(
        Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay) =>
        // 确保是自己打出的卡牌给予格挡，且来源卡牌符合航空分类
        cardSource != null && cardSource.Owner == Owner.Player && props.IsPoweredCardOrMonsterMoveBlock() && IsAffectedCard(cardSource)
            ? 1m + Amount * MultiplierPerStack : 1m;
}

// 空战精英
public sealed class AirCombatElitePower : AviationPower
{
    protected override bool IsAffectedCard(CardModel card) => 
        card.IsAircraftType(IndomitableTags.StrikeFighter, IndomitableKeywords.StrikeFighter);
}

// 雷击精通
public sealed class TorpedoMasteryPower : AviationPower
{
    protected override bool IsAffectedCard(CardModel card) => 
        card.IsAircraftType(IndomitableTags.TorpedoBomber, IndomitableKeywords.TorpedoBomber);
}

// 致命俯冲
public sealed class LethalDivePower : AviationPower
{
    protected override bool IsAffectedCard(CardModel card) => 
        card.IsAircraftType(IndomitableTags.DiveBomber, IndomitableKeywords.DiveBomber);
}

// 焦土轰炸
public sealed class ScorchedBombingPower : AviationPower
{
    protected override bool IsAffectedCard(CardModel card) => 
        card.IsAircraftType(IndomitableTags.LevelBomber, IndomitableKeywords.LevelBomber);
}