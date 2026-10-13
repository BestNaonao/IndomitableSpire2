using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class DynorphinPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    // Amount 是每档损失带来的攻击增幅百分比；同名叠加不延长持续时间。
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner || cardSource == null || cardSource.Owner.Creature != Owner ||
            !props.IsPoweredAttack() || !cardSource.IsCarrierAircraft() || !cardSource.HasDurability()) return 1M;
        var maximum = cardSource.DynamicVars.MaxDurability().BaseValue;
        if (maximum <= 0) return 1M;
        var missing = Math.Clamp(maximum - cardSource.DynamicVars.Durability().BaseValue, 0M, maximum);
        // 读取实际已损失的比例，不读取攻击预览，也不累计历次损失；每 5% 计 Amount%，不要求取整。
        return 1M + Amount * missing / (5M * maximum);
    }
    
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side) await PowerCmd.Remove(this);
    }
}