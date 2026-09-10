using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Hooks;

/// <summary>
/// OnPlay 的异步调用链栈。不可共享可变 Stack：并行选牌分支会继承同一个栈对象。
/// AsyncLocal 仅替换当前链的栈顶，父节点保持不变，await 和递归出牌都能正确溯源。
/// </summary>
internal static class ResonanceContext
{
    private static readonly AsyncLocal<Frame?> Current = new();
    
    public static CardModel? Source
    {
        get
        {
            for (var frame = Current.Value; frame != null; frame = frame.Parent)
            {
                if (frame.IsActive && frame.Card.Keywords.Contains(IndomitableKeywords.Resonance))
                    return frame.Card;
            }
            return null;
        }
    }
    
    public static IDisposable Enter(CardModel card)
    {
        var frame = new Frame(card, Current.Value); // 新帧的父指针指向栈顶
        Current.Value = frame;  // 入栈，成为新的栈顶
        return frame;
    }
    
    private sealed class Frame(CardModel card, Frame? parent) : IDisposable
    {
        public CardModel Card { get; } = card;
        public Frame? Parent { get; } = parent;
        public bool IsActive => !_disposed;
        private volatile bool _disposed;
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;       // 标记失效，防止异步任务延迟执行时错误读取使用。
            Current.Value = Parent;     // 出栈操作，恢复栈顶为父帧。
        }
    }
}