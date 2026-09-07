namespace IndomitableSpire2.IndomitableSpire2Code.Hooks;

/// <summary>
/// 为维护性格挡调整抑制下一次 <see cref="CustomHook.AfterBlockLost"/> 广播。
/// 作用域令牌只会被一条 LoseBlock 指令消费，避免抑制状态传播给该指令内部触发的嵌套指令。
/// </summary>
public static class BlockLossHookSuppression
{
    private static readonly AsyncLocal<SuppressionScope?> CurrentScope = new();
    
    /// <summary>
    /// 抑制当前异步调用链中紧接着执行的一条 CreatureCmd.LoseBlock 的自定义格挡损失广播。
    /// 原版 LoseBlock 行为（包括 AfterBlockBroken）不受影响。
    /// </summary>
    public static IDisposable SuppressNext()
    {
        var scope = new SuppressionScope(CurrentScope.Value);
        CurrentScope.Value = scope;
        return scope;
    }
    
    internal static bool ConsumeForCurrentCommand() => CurrentScope.Value?.TryConsume() == true;
    
    private sealed class SuppressionScope(SuppressionScope? previousScope) : IDisposable
    {
        private int _consumed;
        private int _disposed;
        // 尝试消耗令牌，锁交换保证原子性
        public bool TryConsume() => Interlocked.Exchange(ref _consumed, 1) == 0;
        // 作用域退出时销毁令牌
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            if (ReferenceEquals(CurrentScope.Value, this)) CurrentScope.Value = previousScope;
        }
    }
}