using BaseLib.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class WithererPower : IndomitablePower, IHasSecondAmount
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
    // BeforePowerAmountChanged 能拿到尚未经过其他加成的基础 amount，以及完整的施加上下文。
    // 先记录这些数据，等原效果成功生效后再通过 AfterPowerAmountChanged 额外施加一次。
    public override Task BeforePowerAmountChanged(
        PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        var data = GetInternalData<Data>();
        if (!CanRepeat(power, amount, target, applier, data)) return Task.CompletedTask;
        data.PendingApplications.Add(new PendingApplication(
            power, (PowerModel)power.ClonePreservingMutability(), target, amount, applier, cardSource));
        return Task.CompletedTask;
    }
    
    // 两阶段提交，在第一次施加完成后重复施加
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        var data = GetInternalData<Data>();
        if (data.IsApplyingRepeatedDebuff) return;          // 1. 递归保护，防止无限循环
        
        var pendingIndex = FindPendingApplication(data, power, applier, cardSource);
        if (pendingIndex < 0) return;                       // 2. 如果没有找到记录，则直接返还
        
        var application = data.PendingApplications[pendingIndex];
        data.PendingApplications.RemoveAt(pendingIndex);    // 3. 提取并移除，防止重复消费
        
        // 4. 二次校验：AfterPowerAmountChanged 只会在原效果实际改变了层数后触发；这里再次校验，
        // 避免其他钩子把效果改成非负面效果，或嵌套结算已经用完本回合次数。
        if (!CanRepeat(power, application.BaseAmount, application.Target, applier, data)) return;
        
        data.DebuffsRepeatedThisTurn++;                     // 5. 计数
        data.IsApplyingRepeatedDebuff = true;               // 6. 递归保护
        Flash();
        InvokeDisplayAmountChanged();                       // 7. UI 刷新
        
        // 8. 核心逻辑：使用克隆的 RepeatedPower 和原始的 BaseAmount，正式向目标额外施加一次该 Debuff。
        try
        {
            await PowerCmd.Apply(
                choiceContext,
                application.RepeatedPower,
                application.Target,
                application.BaseAmount,
                application.Applier,
                application.CardSource);
        }
        finally
        {
            // 9. 额外施加本身也会经过同一组原生钩子，无论是否成功，必须在完整结算期间保持此标记。
            data.IsApplyingRepeatedDebuff = false;
        }
    }
    
    // 是否是可重复的施加事件
    private bool CanRepeat(PowerModel power, decimal amount, Creature? target, Creature? applier, Data data) =>
        !data.IsApplyingRepeatedDebuff &&                   // 1. 非递归调用中
        data.DebuffsRepeatedThisTurn < Amount &&            // 2. 本回合已重复次数 < 总能力层数
        Amount > 0 && Owner.IsAlive &&                      // 3. 能力层数大于 0 并且拥有者存活
        IsActive &&                                         // 4. DOT 伤害累计达到阈值
        amount > 0 &&                                       // 5. 施加的数量必须大于 0
        target?.IsEnemy == true && applier == Owner &&      // 6. 必须是拥有者施加给敌人的
        power.GetTypeForAmount(amount) == PowerType.Debuff &&   // 7. 必须是负面效果
        power.StackType == PowerStackType.Counter;          // 8. 必须是可叠加计数的能力类型
    
    // 逆序遍历寻找匹配的施加事件
    private static int FindPendingApplication(Data data, PowerModel power, Creature? applier, CardModel? cardSource)
    {
        for (var i = data.PendingApplications.Count - 1; i >= 0; i--)
        {
            var application = data.PendingApplications[i];
            if (ReferenceEquals(application.OriginalPower, power) &&
                ReferenceEquals(application.Target, power.Owner) &&
                ReferenceEquals(application.Applier, applier) &&
                ReferenceEquals(application.CardSource, cardSource))
            {
                return i;
            }
        }
        return -1;
    }
    
    // ========== 刷新逻辑 ==========
    // 回合开始时，重置本回合的使用次数
    public override Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner))
        {
            var data = GetInternalData<Data>();
            data.DebuffsRepeatedThisTurn = 0;
            data.PendingApplications.Clear();
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
        public bool IsApplyingRepeatedDebuff;   // 递归保护标志
        public readonly List<PendingApplication> PendingApplications = [];  // 待处理队列
    }
    
    private sealed class PendingApplication(PowerModel originalPower, PowerModel repeatedPower, 
        Creature target, decimal baseAmount, Creature? applier, CardModel? cardSource)
    {
        public PowerModel OriginalPower { get; } = originalPower;
        public PowerModel RepeatedPower { get; } = repeatedPower;
        public Creature Target { get; } = target;
        public decimal BaseAmount { get; } = baseAmount;
        public Creature? Applier { get; } = applier;
        public CardModel? CardSource { get; } = cardSource;
    }
}