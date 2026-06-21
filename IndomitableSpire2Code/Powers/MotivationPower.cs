using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class MotivationPower : IndomitablePower
{
    public const int MaxAmount = 100;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new("MotivationAmount", 0M)];
    
    // 1. 【核心映射】：底层 Amount 永远比显示值大 1，防止跌到 0 被引擎移除
    public override int DisplayAmount => Amount - 1;
    public bool IsCompleted => DisplayAmount >= MaxAmount;
    
    // 2. 动态文本切换：根据是否充满，返回不同的本地化键值
    protected override string SmartDescriptionLocKey => IsCompleted
        ? $"{Id.Entry}.smartDescriptionFull"
        : $"{Id.Entry}.smartDescription";
    
    // 3. 首次获得能力：补偿那 1 点的偏移量
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        // 引擎默认按卡牌给的数值设置了 Amount。我们需要默默 +1 垫底。
        SetAmount(Amount + 1, silent: true);
        return Task.CompletedTask;
    }
    
    // 4. 处理下限截断与“满级不掉”的锁死机制
    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower.Id != Id || target != Owner) return false;
        
        // 锁定机制：如果已经满级，且外界试图扣除干劲，则偏移量强行归 0
        if (IsCompleted && amount < 0)
            modifiedAmount = 0M;
        // 下限保护：不能让底层 Amount 跌破 1（否则图标消失），计算刚好跌到 1 的差值
        else if (Amount + amount < 1)
            modifiedAmount = 1 - Amount;
        
        return true;
    }
    
    // 5. 处理上限截断与溢出联动（这是在引擎生效后触发的）
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this) return;
        
        // 如果底层 Amount 超过了最大值 (100 + 1)
        if (Amount > MaxAmount + 1)
        {
            var overflow = Amount - (MaxAmount + 1);
            
            // 默默将数值切回上限，不触发二次特效
            SetAmount(MaxAmount + 1, silent: true);
            
            // 【核心修改】：彻底解耦！不再去寻找 MotivationBurstPower，而是向全场广播“我溢出了！”
            await CustomHook.AfterResourceOverflowed(
                choiceContext, Owner, this, overflow, applier, cardSource);
        }
        
        // 6. 更新动态变量，仅供本地化文本渲染，兼顾施加和追加
        DynamicVars["MotivationAmount"].BaseValue = DisplayAmount;
    }
    
    // 供其他卡牌/遗物归零使用的快捷方法
    public void Restart()
    {
        var actualChange = -DisplayAmount;
        if (actualChange == 0) return;
        
        // 默默归底
        SetAmount(1, silent: true);
        DynamicVars["MotivationAmount"].BaseValue = DisplayAmount;
        
        // 手动触发原版的红字减少特效
        Owner.InvokePowerModified(this, actualChange, silent: false);
        Flash();
    }
}