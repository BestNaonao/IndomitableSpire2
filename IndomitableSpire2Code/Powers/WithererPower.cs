using BaseLib.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class WithererPower : IndomitablePower, IHasSecondAmount
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override object InitInternalData() => new Data();
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new("Threshold", ScaledThreshold)];
    
    // ========== UI 与进度展示 ==========
    // 获取当前玩家的起火+进水总伤害
    private int TotalDotDamage => Witherer.DealtDotDamage(Owner.Player);
    private int ScaledThreshold => Witherer.ScaledThreshold(CombatState);
    
    // 是否激活
    private bool IsActive => TotalDotDamage >= ScaledThreshold;
    
    // First Amount: 本回合剩余可用次数
    public override int DisplayAmount => Math.Max(0, Amount - GetInternalData<Data>().DebuffsMultipliedThisTurn);
    
    // Second Amount: 展示伤害累计进度，满了显示 "MAX"
    public string GetSecondAmount() => IsActive ? "MAX" : $"{TotalDotDamage}/{ScaledThreshold}";
    
    // 根据是否激活，动态切换本地化文本
    protected override string SmartDescriptionLocKey => 
        IsActive ? $"{Id.Entry}.smartDescriptionActive" : $"{Id.Entry}.smartDescriptionInactive";
    
    // ========== 核心机制：拦截与翻倍负面效果 ==========
    // 【关键钩子】：在给予能力数值之前被调用（此时只是构建意图，尚未物理生效）
    public override decimal ModifyPowerAmountGivenAdditive(
        PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        // 1. 基本安全校验：自身存活，有施加数值（不能翻倍层数减少的效果）
        if (Amount <= 0 || Owner.IsDead || amount <= 0 || target == null || cardSource == null) return 0M;
        
        // 2. 状态校验：必须由拥有者本人（玩家）施加，且能力已激活，且目标是敌人
        if (giver != Owner || !IsActive || !target.IsEnemy) return 0M;
        
        // 3. 拦截特定目标：我们只翻倍叠加规则为 Counter 的【负面效果(Debuff)】
        if (power.GetTypeForAmount(amount) != PowerType.Debuff || power.StackType != PowerStackType.Counter) return 0M;
        
        // 4. 次数与类型校验：确认本回合的剩余翻倍次数，有剩余次数则返回额外的 Additive 增量实现翻倍
        return GetInternalData<Data>().DebuffsMultipliedThisTurn < Amount ? amount : 0M;
    }
    
    // 当引擎最终将这个翻倍后的数值给出去后，我们在这里扣除内部次数
    public override async Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        Flash();
        GetInternalData<Data>().DebuffsMultipliedThisTurn++;
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }
    
    // ========== 回合刷新 ==========
    // 回合开始时，重置本回合的使用次数
    public override Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner))
        {
            GetInternalData<Data>().DebuffsMultipliedThisTurn = 0;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }
    
    private class Data
    {
        public int DebuffsMultipliedThisTurn;
    }
}