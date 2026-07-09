using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class MotivationPower : DynamicVarSyncPower
{
    public const int MaxAmount = 100;
    private const int BaseFloor = 1;
    private const int TrueMaxAmount = MaxAmount + BaseFloor;
    
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 实例化内部数据，用于在同步和异步钩子之间传递参数
    protected override object InitInternalData() => new MotivationData();
    
    private class MotivationData
    {
        public decimal PendingOverflow;
        public Creature? LastApplier;
        public CardModel? LastCardSource;
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new("MotivationAmount", 0M)];
    
    public override int DisplayAmount => Amount - BaseFloor;
    public bool IsCompleted => DisplayAmount >= MaxAmount;
    
    // 2. 动态文本切换：根据是否充满，返回不同的本地化键值
    protected override string SmartDescriptionLocKey => IsCompleted
        ? $"{Id.Entry}.smartDescriptionFull"
        : $"{Id.Entry}.smartDescription";
    
    // 3. 首次获得能力：补偿那 1 点的偏移量
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        // 引擎默认按卡牌给的数值设置了 Amount。我们需要默默 +1 垫底。
        SetAmount(Amount + BaseFloor, silent: true);
        return Task.CompletedTask;
    }
    
    // 顺序 1：BeforePowerAmountChanged (记录上下文)：截获当前的触发源和卡牌，为稍后的异步溢出广播做准备
    public override Task BeforePowerAmountChanged(
        PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            var data = GetInternalData<MotivationData>();
            data.LastApplier = applier;
            data.LastCardSource = cardSource;
        }
        return Task.CompletedTask;
    }
    
    // 顺序 4：TryModifyPowerAmountReceived (截断数值 & 计算溢出)
    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower.Id != Id || target != Owner) return false;
        
        // 满级锁定：根据设定，满级时不掉干劲
        if (IsCompleted && amount < 0)
            modifiedAmount = 0M;
        else
        {
            var prospective = Amount + amount;
            // 越界：上限溢出：计算出纯粹的溢出量并暂存，然后强行截断
            if (prospective > TrueMaxAmount)
            {
                var overflow = prospective - TrueMaxAmount;
                GetInternalData<MotivationData>().PendingOverflow += overflow;
                modifiedAmount = TrueMaxAmount - Amount;
            }
            // 越界：下限保护
            else if (prospective < BaseFloor)
            {
                modifiedAmount = BaseFloor - Amount;
            }
        }
        // 只要返回 true，引擎就会确信我们干预了数值，并会在稍后触发 AfterModifying 钩子
        return modifiedAmount != amount;
    }
    
    // 顺序 6：AfterModifyingPowerAmountReceived (发送异步溢出广播)
    public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        if (power != this) return;
        if (GetInternalData<MotivationData>() is { PendingOverflow: > 0 } data)
        {
            var overflow = data.PendingOverflow;
            data.PendingOverflow = 0;   // 取出后立刻清空
            await CustomHook.AfterResourceOverflowed(
                new ThrowingPlayerChoiceContext(), Owner, this, overflow, 
                data.LastApplier, data.LastCardSource); // 完美执行异步广播！
        }
    }
    
    // 实现基类的抽象方法
    protected override void SyncDynamicVars() => DynamicVars["MotivationAmount"].BaseValue = DisplayAmount;
    
    // 供其他卡牌/遗物归零使用的快捷方法
    public void Restart()
    {
        if (-DisplayAmount is var actualChange && actualChange == 0) return;
        
        // 默默归底，并手动触发原版的红字减少特效
        SetAmount(BaseFloor, silent: true);
        SyncDynamicVars();
        Owner.InvokePowerModified(this, actualChange, silent: false);
        Flash();
    }
}