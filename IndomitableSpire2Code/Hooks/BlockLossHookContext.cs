using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace IndomitableSpire2.IndomitableSpire2Code.Hooks;

/// <summary>
/// 为紧接着的一次 CreatureCmd.LoseBlock 提供额外语义来源。
/// 令牌绑定目标且只能消费一次，避免原因传播给该指令内部触发的嵌套指令。
/// </summary>
internal static class BlockLossHookContext
{
    private static readonly AsyncLocal<ReasonScope?> CurrentScope = new();
    
    // 为下一条 LoseBlock 指令提供语义来源
    internal static IDisposable UseReasonForNext(Creature target, BlockLossReason reason)
    {
        var scope = new ReasonScope(CurrentScope.Value, target, reason);
        CurrentScope.Value = scope;
        return scope;
    }
    
    internal static BlockLossReason ConsumeReasonForCurrentCommand(Creature target)
    {
        // 嵌套作用域可能属于不同目标；查找第一个匹配目标且尚未消费的令牌。
        for (var scope = CurrentScope.Value; scope != null; scope = scope.PreviousScope)
        {
            if (scope.TryConsume(target, out var reason)) return reason;
        }
        return BlockLossReason.Normal;
    }
    
    private sealed class ReasonScope(ReasonScope? previousScope, Creature target, BlockLossReason reason) : IDisposable
    {
        private int _consumed;
        private int _disposed;
        
        public ReasonScope? PreviousScope => previousScope;
        private bool IsDisposed => Volatile.Read(ref _disposed) != 0;   // 确保读取到的是最新的值
        
        // 尝试消费令牌，返回作用域的语义来源，必须是未销毁、目标匹配、未消费的令牌，CAS 保证原子性和一次性。
        public bool TryConsume(Creature commandTarget, out BlockLossReason consumedReason)
        {
            consumedReason = reason;
            return !IsDisposed && ReferenceEquals(target, commandTarget) && 
                   Interlocked.CompareExchange(ref _consumed, 1, 0) == 0;
        }
        
        // 安全地退出作用域并恢复状态。
        public void Dispose()
        {
            // 已销毁或本作用域不位于栈顶，则不越权修改全局状态。
            if (Interlocked.Exchange(ref _disposed, 1) != 0 || !ReferenceEquals(CurrentScope.Value, this)) return;
            // 弹性恢复，跳过已销毁的父节点，找到最近的未销毁祖先节点作为栈顶作用域。
            var nextScope = previousScope;
            while (nextScope?.IsDisposed == true)
                nextScope = nextScope.PreviousScope;
            CurrentScope.Value = nextScope;
        }
    }
}