using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class MotivationPower : IndomitablePower // 继承自你的能力基类
{
    public const int MaxAmount = 100;
    public const int TransProportion = 10;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 1. 注册动态变量 MotivationAmount，初始值为 0
    protected override IEnumerable<DynamicVar> CanonicalVars => [new("MotivationAmount", 0M)];
    
    // 2. 显示层数直接绑定到动态变量
    public override int DisplayAmount => DynamicVars["MotivationAmount"].IntValue;
    
    public bool IsCompleted => DisplayAmount >= MaxAmount;
    
    // 3. 动态文本切换：根据是否充满，返回不同的本地化键值
    protected override string SmartDescriptionLocKey => IsCompleted
        ? $"{Id.Entry}.smartDescriptionFull"
        : $"{Id.Entry}.smartDescription";
    
    // 4. 处理首次获得该能力的情况 (接管初始 Amount)
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        // 如果是战斗开始的初始化 (你代码里写的 Apply 1)，代表 0 干劲，保持 MotivationAmount 为 0。
        // 但如果未来有卡牌直接赋予了多层（比如直接 Apply 5），我们要将其吸收进动态变量。
        var amount = Amount;
        if (amount > 1)
            DynamicVars["MotivationAmount"].BaseValue = Math.Min(amount, MaxAmount);
        
        // 彻底锁死引擎层面的 Amount 为 1，保证能力图标永远不会被销毁
        SetAmount(1, silent: true);
        InvokeDisplayAmountChanged();
        
        // 事后处理，确保活力图标出现在干劲图标出现后
        _ = AfterAmountChanged(amount, applier, null);
        return Task.CompletedTask;
    }
    
    // 5. 拦截后续所有对该能力的层数修改 (PowerCmd.Apply)
    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        
        // 确保拦截的是我们自己，且目标是拥有者
        if (canonicalPower.Id != Id || target != Owner) return false;
        
        // 强制 modifiedAmount 为 0，保护底层的 Amount 永远不被修改
        modifiedAmount = 0M;
        
        // 已满且试图减少，直接屏蔽
        if (IsCompleted && amount < 0) return true;
        
        // 计算实际的增减量（防止溢出 100 或跌破 0 导致的特效数值错误）
        var oldAmount = DynamicVars["MotivationAmount"].BaseValue;
        var newAmount = Math.Clamp(oldAmount + amount, 0M, MaxAmount);
        var actualChange = (int)(newAmount - oldAmount);
        
        // 如果实际数值确实发生了变化，则手动触发引擎的标准 VFX 和 SFX 广播。
        if (actualChange != 0)
        {
            DynamicVars["MotivationAmount"].BaseValue = newAmount;
            InvokeDisplayAmountChanged();
            Owner.InvokePowerModified(this, actualChange, silent: false);
        }
        
        // 事后处理，确保活力图标出现在干劲图标出现后
        _ = AfterAmountChanged(oldAmount + amount, applier, null);
        return true;
    }
    
    private async Task AfterAmountChanged(decimal proposed, Creature? applier, CardModel? cardSource)
    {
        if (proposed > MaxAmount)
        {
            var vigorToApply = Math.Floor((proposed - MaxAmount) / TransProportion);
            if (vigorToApply > 0) 
                await PowerCmd.Apply<VigorPower>(Owner, vigorToApply, applier, cardSource);
        }
    }
    
    // 供其他卡牌或遗物调用的归零使用的快捷方法
    public void Restart()
    {
        var actualChange = -DisplayAmount;
        if (actualChange == 0) return;
        DynamicVars["MotivationAmount"].BaseValue = 0M;
        InvokeDisplayAmountChanged();
        // 归零时同样触发一次“减少”的特效和音效
        Owner.InvokePowerModified(this, actualChange, silent: false);
        Flash();
    }
}