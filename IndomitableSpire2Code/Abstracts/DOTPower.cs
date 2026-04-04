using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

public abstract class DOTPower : IndomitablePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public virtual decimal Proportion => 0.01m;
    
    // 注册需要精确维护的四个动态变量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new("TotalLostHpExact", 0m), // 总累计小数伤害（后台静默运行）
        new("TotalLostHpInt", 0m),   // 总累计已扣除的整数伤害
        new("NextLostHpPercent", 0m),// 下次损失百分比（用于UI显示）
        new("NextLostHpInt", 0m)     // 下次即将损失的整数生命（用于UI显示）
    ];
    
    // 获取下一次即将造成的真实整数伤害
    public int GetNextDamage()
    {
        if (Owner.MaxHp <= 0) return 0;
        var exactNext = Owner.MaxHp * Amount * Proportion; // 百分比 * 层数
        return (int)Math.Floor(DynamicVars["TotalLostHpExact"].BaseValue + exactNext) - (int)DynamicVars["TotalLostHpInt"].BaseValue;
    }
    
    // 更新UI变量的方法
    protected virtual void Update()
    {
        DynamicVars["NextLostHpPercent"].BaseValue = Amount * 100 * Proportion;
        DynamicVars["NextLostHpInt"].BaseValue = GetNextDamage();
        InvokeDisplayAmountChanged();
    }
    
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Update();
        return Task.CompletedTask;
    }
    
    // 拦截层数变化，实时更新UI预览
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower.Id != Id || target != Owner) return false;
        // 预测未来的层数并更新UI
        var nextAmount = Amount + amount;
        DynamicVars["NextLostHpPercent"].BaseValue = nextAmount * 100 * Proportion;
        var exactNext = Owner.MaxHp * nextAmount * Proportion;
        DynamicVars["NextLostHpInt"].BaseValue = Math.Floor(DynamicVars["TotalLostHpExact"].BaseValue + exactNext) - DynamicVars["TotalLostHpInt"].BaseValue;
        return false;
    }
    
    // 核心伤害逻辑提取
    protected virtual async Task TriggerDamage()
    {
        if (Amount <= 0 || Owner.IsDead) return;

        // 1. 计算精确伤害并入池
        var exactNext = Owner.MaxHp * Amount * Proportion;
        DynamicVars["TotalLostHpExact"].BaseValue += exactNext;
        
        // 2. 提取需要扣除的整数部分
        var damageToDeal = (int)Math.Floor(DynamicVars["TotalLostHpExact"].BaseValue) - (int)DynamicVars["TotalLostHpInt"].BaseValue;

        if (damageToDeal > 0)
        {
            DynamicVars["TotalLostHpInt"].BaseValue += damageToDeal;
            // 造成无视格挡、不受力量影响的绝对伤害（模仿 Poison）
            Flash();
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, damageToDeal, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        }

        // 3. 层数衰减与UI更新
        if (Owner.IsAlive)
        {
            await PowerCmd.Decrement(this);
            Update();
        }
        else await Cmd.CustomScaledWait(0.1f, 0.25f);
    }

    // 回合开始时触发一次
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side) await TriggerDamage();
    }

    // 回合结束时再触发一次
    public override async Task BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side) await TriggerDamage();
    }
}