using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Hooks;

/// <summary>回光补丁共享的临时数据区；每张卡牌独立记录，避免嵌套打出和多人出牌串用状态。</summary>
internal static class AfterglowContext
{
    // 弱引用键不阻止卡牌回收；结算 finally 中主动移除。运行中状态不保存、不随克隆复制。
    internal static readonly ConditionalWeakTable<CardModel, Data> ActiveCards = new();
    
    internal sealed class Data
    {
        public bool PlayingBeforeExhaust = true;
        public bool FinalizingExhaust;
    }
    
    internal static bool IsPlaying(CardModel card) =>
        ActiveCards.TryGetValue(card, out var data) && data.PlayingBeforeExhaust;
}
