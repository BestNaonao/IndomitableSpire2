using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class IndomitablePrayerPower : IndomitablePower, IHasSecondAmount
{
    private const string DamageCapKey = "DamageCap";
    private const string HasDamageCapKey = "HasDamageCap";
    
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new(DamageCapKey, 0M), new BoolVar(HasDamageCapKey)];
    
    /// <summary>
    /// 本场战斗中持有者最近一次受到的总伤害（包括被格挡的部分）。
    /// null 表示尚无伤害记录；0 是有效上限，不能视为“未生效”。
    /// </summary>
    public int? NextDamageCap => IsMutable ? ReadLastDamage(Owner) : null;
    
    private static int? ReadLastDamage(Creature target) => CombatManager.Instance.History.Entries
        .OfType<DamageReceivedEntry>()
        .LastOrDefault(entry => entry.Receiver == target)?.Result.TotalDamage;
    
    public string GetSecondAmount() => ((BoolVar)DynamicVars[HasDamageCapKey]).BoolVal
        ? DynamicVars[DamageCapKey].IntValue.ToString()
        : string.Empty;
    
    private void SyncDamageCap(Creature target)
    {
        var cap = ReadLastDamage(target);
        var hasCap = (BoolVar)DynamicVars[HasDamageCapKey];
        var damageCap = DynamicVars[DamageCapKey];
        // 通过 Early Return 避免不必要的 UI 刷新
        if (hasCap.BoolVal == cap.HasValue && damageCap.BaseValue == (cap ?? 0)) return;
        hasCap.BoolVal = cap.HasValue;
        damageCap.BaseValue = cap ?? 0;
        this.InvokeSecondAmountChanged();
    }
    
    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // Owner 此时尚未设置，先用 target 初始化，避免图标短暂显示错误的数值。
        SyncDamageCap(target);
        return Task.CompletedTask;
    }
    
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncDamageCap(Owner);
        return Task.CompletedTask;
    }
    
    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 重复施加只刷新显示；Amount 不参与限伤，也不延长持续时间。
        if (power == this) SyncDamageCap(Owner);
        return Task.CompletedTask;
    }
    
    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        // 此时历史已写入。重新读取最近记录，避免连锁伤害结束后被较早的 result 覆盖。
        if (target == Owner) SyncDamageCap(Owner);
        return Task.CompletedTask;
    }
    
    public override decimal ModifyDamageCap(
        Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // 在扣除格挡前限伤；不筛选攻击、来源或当前回合。实际结算和预览均读取最新历史，应避免使用过期缓存。
        return target == Owner && NextDamageCap is { } cap ? cap : decimal.MaxValue;
    }
    
    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        Flash();
        return Task.CompletedTask;
    }
    
    public override async Task AfterSideTurnEndLate(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // 覆盖本轮敌方攻击与普通回合结束触发；一次性移除，不按 Amount 延长。
        if (side == CombatSide.Enemy) await PowerCmd.Remove(this);
    }
}