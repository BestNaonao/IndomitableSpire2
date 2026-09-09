using BaseLib.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class WithererPower : IndomitablePower, IHasSecondAmount, IPowerApplyCountModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override object InitInternalData() => new Data();
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ThresholdVar(9999M)];
    
    // ========== UI 与进度展示 ==========
    // 获取当前玩家的起火+进水总伤害
    private int TotalDotDamage => Witherer.DealtDotDamage(Owner.Player);
    private int ScaledThreshold => Witherer.ScaledThreshold(CombatState);
    
    // 施加后修改门槛，因为在初始化 CanonicalVars 时调用 CombatState 会因为 Owner 未初始化而报错。
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        DynamicVars.Threshold().BaseValue = ScaledThreshold;
        // 订阅追踪器的数据更新事件：先退订防重复，再订阅 InvokeDisplayAmountChanged
        if (Owner.Player?.PlayerCombatState is { } combatState && 
            PlayerCombatStateTrackerExtensions.PowerDamageTracker.Get(combatState) is { } tracker)
        {
            tracker.OnDamageUpdated -= InvokeDisplayAmountChanged;
            tracker.OnDamageUpdated += InvokeDisplayAmountChanged;
        }
        return Task.CompletedTask;
    }
    
    // 是否激活
    private bool IsActive => TotalDotDamage >= ScaledThreshold;
    
    // First Amount: 本回合剩余可用次数
    public override int DisplayAmount => Math.Max(0, Amount - GetInternalData<Data>().DebuffsRepeatedThisTurn);
    
    // Second Amount: 展示伤害累计进度，满了显示 "✓️"
    public string GetSecondAmount() => IsActive ? "✓️" : $"{TotalDotDamage}/{ScaledThreshold}";
    
    // 根据是否激活，动态切换本地化文本
    protected override string SmartDescriptionLocKey => 
        IsActive ? $"{Id.Entry}.smartDescriptionActive" : $"{Id.Entry}.smartDescriptionInactive";
    
    // ========== 核心机制：额外给予一次负面效果 ==========
    // 与原版卡牌重放一样，只在原始请求开始时给总次数 +1，不依赖能力是否被人工制品挡住。
    public int ModifyPowerApplyCount(
        PowerModel power, Creature target, decimal amount, Creature? applier, CardModel? cardSource, int applyCount)
    {
        var data = GetInternalData<Data>();
        return CanRepeat(power, amount, target, applier, data) ? checked(applyCount + 1) : applyCount;
    }
    
    // 只有实际改变次数才会收到一次此通知，在执行任何施加包前消费本回合配额。
    public Task AfterModifyingPowerApplyCount(
        PlayerChoiceContext choiceContext, PowerModel power, Creature target,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        GetInternalData<Data>().DebuffsRepeatedThisTurn++;
        Flash();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
    
    // 是否是可重复的施加事件
    private bool CanRepeat(PowerModel power, decimal amount, Creature? target, Creature? applier, Data data) =>
        data.DebuffsRepeatedThisTurn < Amount &&
        Amount > 0 && Owner.IsAlive && IsActive &&
        amount > 0 && target?.IsEnemy == true && applier == Owner &&
        power.GetTypeForAmount(amount) == PowerType.Debuff &&
        power.StackType == PowerStackType.Counter;
    
    // ========== 刷新逻辑 ==========
    // 回合开始时，重置本回合的使用次数
    public override Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner))
        {
            var data = GetInternalData<Data>();
            data.DebuffsRepeatedThisTurn = 0;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }
    
    // 【规范】：能力被移除时（或战斗结束时）退订事件，防止内存泄漏
    public override Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.Player?.PlayerCombatState is { } combatState && 
            PlayerCombatStateTrackerExtensions.PowerDamageTracker.Get(combatState) is { } tracker)
        {
            tracker.OnDamageUpdated -= InvokeDisplayAmountChanged;
        }
        return base.AfterRemoved(oldOwner);
    }
    
    private class Data
    {
        public int DebuffsRepeatedThisTurn;     // 本回合已经触发重复施加的次数
    }
}