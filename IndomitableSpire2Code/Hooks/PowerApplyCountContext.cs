using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Hooks;

/// <summary>
/// 隔离异步施加链。原生调用令牌只放行本条命令及 Apply 内部的一次 ModifyAmount 转发；
/// 首包的其他嵌套请求仍可独立生成次数，额外包及其派生效果则禁止链式重复。
/// </summary>
internal static class PowerApplyCountContext
{
    private static readonly AsyncLocal<NativeCommand?> CurrentCommand = new();
    private static readonly AsyncLocal<int> SuppressionDepth = new();
    
    // 根据深度判断当前上下文是否处于抑制状态
    internal static bool IsSuppressed => SuppressionDepth.Value > 0;
    
    internal static IDisposable SuppressCountGeneration()
    {
        var previous = SuppressionDepth.Value;
        SuppressionDepth.Value = previous + 1;      // 抑制的深度 +1，进入抑制状态
        return new Scope(() => SuppressionDepth.Value = previous);  // 返回一个 IDisposable 作用域，销毁时深度 -1
    }
    
    // 获取令牌，设置当前原生命令。区别于 SuppressCountGeneration，只设置命令，所以应与 SuppressCountGeneration 成对使用。
    internal static IDisposable UseNativeCommand(bool isApply, PlayerChoiceContext choiceContext, PowerModel power, 
        Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        var previous = CurrentCommand.Value;    // 记录之前的命令，再封装和设置当前的新命令
        CurrentCommand.Value = new NativeCommand(isApply, choiceContext, power, target, amount, applier, cardSource);
        return new Scope(() => CurrentCommand.Value = previous);    // 返回作用域作为令牌，销毁时恢复之前的命令
    }
    
    // 验证令牌，尝试消费原生命令
    internal static bool TryConsumeNativeCommand(bool isApply, PlayerChoiceContext choiceContext, PowerModel power, 
        Creature target, decimal amount, Creature? applier, CardModel? cardSource) =>
        CurrentCommand.Value?.TryConsume(isApply, choiceContext, power, target, amount, applier, cardSource) == true;
    
    // 内部类：封装原生命令的参数和消费逻辑
    private sealed class NativeCommand(bool isApply, PlayerChoiceContext choiceContext, PowerModel power, 
        Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        private bool _expectsApply = isApply;           // 期望操作类型（Apply 或 ModifyAmount）
        private PowerModel? _expectedPower = power;     // 期望能力实例
        
        // 尝试消费令牌，操作类型、上下文、能力实例、施加目标、施加数量、施加者、施加卡牌源都必须一致。
        internal bool TryConsume(bool commandIsApply, PlayerChoiceContext commandContext, PowerModel commandPower, 
            Creature commandTarget, decimal commandAmount, Creature? commandApplier, CardModel? commandCardSource)
        {
            if (_expectedPower == null || _expectsApply != commandIsApply ||
                !ReferenceEquals(choiceContext, commandContext) || !ReferenceEquals(_expectedPower, commandPower) ||
                !ReferenceEquals(target, commandTarget) || amount != commandAmount ||
                !ReferenceEquals(applier, commandApplier) || !ReferenceEquals(cardSource, commandCardSource)) return false;
            
            // 特殊后门逻辑：原版 Apply 在第一个 await 前，会查找已存在的叠加实例，并转而调用 ModifyAmount。
            // 我们只允许这个“精确的转发”通过一次校验。
            // 如果是 Apply 操作，消费后，将期望的 Power 更新为“已存在的叠加实例”（FindExistingInstanceForStacking），
            // 并将期望操作设为 false (ModifyAmount)。
            // 这样，紧接着发生的 ModifyAmount 调用就能通过下一次的 TryConsume 校验。
            _expectedPower = commandIsApply ? PowerCmd.FindExistingInstanceForStacking(commandPower, target, applier) : null;
            _expectsApply = false;
            return true;
        }
    }
    
    // 内部类：标准的 IDisposable 作用域管理器。
    private sealed class Scope(Action restore) : IDisposable
    {
        private bool _disposed;
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            restore();  // 执行传入的恢复动作（如恢复 SuppressionDepth 或 CurrentCommand）
        }
    }
}