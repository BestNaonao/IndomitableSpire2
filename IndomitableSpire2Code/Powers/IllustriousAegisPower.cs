using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class IllustriousAegisPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 【关键1】：允许该能力在同一个生物身上存在多个独立的实例
    public override bool IsInstanced => true;
    
    // 注册内部变量以绑定本地化文本，便于展示回血量
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(0M)];
    
    // 【关键2】：使用内部数据类存储每个实例的独特回血量
    protected override object InitInternalData() => new AegisData();
    
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        // 初始化时，从打出它的卡牌上获取治疗量并存入内部数据
        if (cardSource != null && cardSource.DynamicVars.TryGetValue(HealVar.defaultName, out var healVar))
        {
            var healValue = (int)healVar.BaseValue;
            GetInternalData<AegisData>().HealAmount = healValue;
            DynamicVars.Heal.BaseValue = healValue; // 同步至 UI 变量
        }
        return Task.CompletedTask;
    }
    
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var data = GetInternalData<AegisData>();
        var damageToAbsorb = result.BlockedDamage;
        
        // 【核心同步机制】：消耗由上一个领袖传递过来的“已处理伤害”欠条
        // 这样即使本实例意外成为新领袖，它也会把已经被老领袖处理过的伤害抹除。
        if (data.HandledDamage > 0)
            damageToAbsorb -= Math.Min(damageToAbsorb, data.HandledDamage);
        data.HandledDamage = 0;
        
        // 如果伤害已经被完全抵消、不是正常攻击、或者打的不是自己，则跳过
        if (target != Owner || damageToAbsorb <= 0 || !props.IsPoweredAttack()) return;
        
        // 领袖模式：为了防止各个实例重复扣除，仅由当前存活的最老实例统筹
        var allInstances = target.Powers.OfType<IllustriousAegisPower>().ToList();
        if (allInstances.Count == 0 || allInstances[0] != this) return;
        
        // 记录本次领袖将要统筹处理的总伤害，用于通知后方的实例
        var totalDamageProcessedByLeader = damageToAbsorb;
        
        // 依次遍历所有实例进行结算
        foreach (var aegis in allInstances)
        {
            // 【关键广播】：给后面的小弟塞欠条！告诉它们这个事件的伤害我已经处理了。
            if (aegis != this)
                aegis.GetInternalData<AegisData>().HandledDamage += totalDamageProcessedByLeader;

            // 注意这里不能用 break，必须用 continue，以确保所有排在后面的实例都能收到欠条！
            if (damageToAbsorb <= 0) continue; 

            var absorb = Math.Min(aegis.Amount, damageToAbsorb);
            damageToAbsorb -= absorb;
            var newAmount = await PowerCmd.ModifyAmount(aegis, -absorb, dealer, cardSource);

            if (newAmount > 0 || aegis.GetInternalData<AegisData>().HealAmount is not (var healAmt and > 0)) continue;
            aegis.Flash();
            await CreatureCmd.Heal(Owner, healAmt);
        }
    }
    
    public override bool ShouldClearBlock(Creature creature) => Owner != creature;
    
    public void OnRetentionTriggered(AbstractModel sourceModel, Creature creature)
    {
        Flash();
    }
    
    private class AegisData
    {
        public int HealAmount { get; set; }
        public int HandledDamage { get; set; } // 用于记录在同一次伤害事件中，已经被领袖代为处理过的伤害量
    }
}