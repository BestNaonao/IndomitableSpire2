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
    /// 触发加成所需的卡牌标签。基类默认为“舰载机”总标签。
    /// </summary>
    protected virtual CardTag RequiredTag => IndomitableTags.CarrierAircraft;
    
    // 拦截伤害与格挡的乘区计算
    public override decimal ModifyDamageMultiplicative(
        Creature? target, 
        decimal amount, 
        ValueProp props, 
        Creature? dealer, 
        CardModel? cardSource) => 
        // 确保攻击者是自己，伤害属于正常受力量加成的攻击，且来源卡牌包含指定的机种标签
        dealer == Owner && props.IsPoweredAttack() && 
        cardSource.IsCarrierAircraft() && cardSource!.Tags.Contains(RequiredTag)
            ? 1m + Amount * MultiplierPerStack  // 返回 1 + 层数 * 每层比例，即 1 + 层数%
            : 1m;
    
    public override decimal ModifyBlockMultiplicative(
        Creature target, 
        decimal block, 
        ValueProp props, 
        CardModel? cardSource, 
        CardPlay? cardPlay) =>
        // 确保获得格挡的是自己，且来源卡牌包含指定的机种标签
        target == Owner && props.IsPoweredCardOrMonsterMoveBlock() && 
        cardSource.IsCarrierAircraft() && cardSource!.Tags.Contains(RequiredTag)
            ? 1m + Amount * MultiplierPerStack
            : 1m;
}

// 空战精英
public sealed class AirCombatElitePower : AviationPower
{
    protected override CardTag RequiredTag => IndomitableTags.StrikeFighter;
}

// 雷击精通
public sealed class TorpedoMasteryPower : AviationPower
{
    protected override CardTag RequiredTag => IndomitableTags.TorpedoBomber;
}

// 致命俯冲
public sealed class LethalDivePower : AviationPower
{
    protected override CardTag RequiredTag => IndomitableTags.DiveBomber;
}

// 焦土轰炸
public sealed class ScorchedBombingPower : AviationPower
{
    protected override CardTag RequiredTag => IndomitableTags.LevelBomber;
}