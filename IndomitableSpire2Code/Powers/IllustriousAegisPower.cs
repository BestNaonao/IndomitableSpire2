using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class IllustriousAegisPower : IndomitablePower, IHasSecondAmount
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 【关键1】：允许该能力在同一个生物身上存在多个独立的实例
    public override bool IsInstanced => true;
    
    // 注册内部变量以绑定本地化文本，便于展示回血量
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(0M)];
    
    public string GetSecondAmount() => DynamicVars.Heal.ToString();
    
    // 【关键2】：使用内部数据类存储每个实例的独特回血量
    protected override object InitInternalData() => new AegisData();
    
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        // 初始化时，从打出它的卡牌上获取治疗量并存入内部数据
        if (cardSource != null && cardSource.DynamicVars.TryGetValue(HealVar.defaultName, out var healVar))
        {
            GetInternalData<AegisData>().HealAmount = healVar.IntValue;
            DynamicVars.Heal.BaseValue = healVar.BaseValue; // 同步至 UI 变量
        }
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
    
    // 【核心钩子】：当其他能力（特指 ShieldPower）数值改变时被调用
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 1. 【完美解耦】：独立监听自身的碎裂事件：如果改变的是自己，且层数降到了 0 及以下，且是负向改变（扣除）
        if (power == this && Amount <= 0 && amount < 0 && GetInternalData<AegisData>().HealAmount is var healAmt and > 0)
        {
            // 此时能力还在身上，尚未被引擎完全摘除，Flash 特效完美触发！
            Flash();
            await CreatureCmd.Heal(Owner, healAmt);
            return;
        }
        
        // 2. 监听宿主身上 ShieldPower 的损耗
        if (power.Owner != Owner || power is not ShieldPower || amount >= 0) return;
        
        var data = GetInternalData<AegisData>();
        
        // 【领袖同步与消耗欠条】：这部分逻辑保留，用于处理同一个受击事件中的多重实例结算
        var damageReduced = -(int)amount; // Shield 减少的量就是伤害吸收量
        
        if (data.HandledDamage > 0)
            damageReduced -= Math.Min(damageReduced, data.HandledDamage);
        data.HandledDamage = 0;
        
        if (damageReduced <= 0) return;
        var totalDamageProcessedByLeader = damageReduced;
        
        // 领袖模式统筹分配
        var allInstances = Owner.Powers.OfType<IllustriousAegisPower>().ToList();
        if (allInstances.Count == 0 || allInstances[0] != this) return;
        foreach (var aegis in allInstances)
        {
            if (aegis != this)
                aegis.GetInternalData<AegisData>().HandledDamage += totalDamageProcessedByLeader;
            
            if (damageReduced <= 0) continue;
            
            var absorb = Math.Min(aegis.Amount, damageReduced);
            damageReduced -= absorb;
            
            // 扣除当前遍历庇护实例的层数，通过引擎广播该实例的 AfterPowerAmountChanged，触发 Flash 和回血
            await PowerCmd.ModifyAmount(aegis, -absorb, applier, cardSource);
        }
    }
    
    // 内部数据类
    private class AegisData
    {
        public int HealAmount { get; set; }
        public int HandledDamage { get; set; } // 用于记录在同一次伤害事件中，已经被领袖代为处理过的伤害量
    }
}