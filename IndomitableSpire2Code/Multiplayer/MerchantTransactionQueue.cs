using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Multiplayer;

/// <summary>同一玩家的付费购买及其嵌套赠品必须完成后，才能处理下一条购买消息。</summary>
public static class MerchantTransactionQueue
{
    private sealed class QueueState
    {
        public readonly Dictionary<MerchantInventory, Task> Tails = []; // 玩家购买链的尾任务，按库存（即按玩家）分链
        public bool Closing; // 整个商店离店级别的标志，跨所有玩家库存生效，离店排水时置 true
    }
    
    private static readonly ConditionalWeakTable<MerchantSynchronizer, QueueState> Queues = new(); // 按同步器（每局一个）
    
    public static bool IsClosing(MerchantSynchronizer sync) => Queues.TryGetValue(sync, out var state) && state.Closing;
    
    /// <summary>同步建链入口。两条购买消息即使几乎同时到达，建链顺序也在接收时即确定，不会乱序。</summary>
    public static Task<bool> Purchase(MerchantSynchronizer sync, MerchantInventory inventory, uint entryId)
    {
        var state = Queues.GetOrCreateValue(sync);
        var previous = state.Tails.GetValueOrDefault(inventory, Task.CompletedTask); // 获取当前尾任务，链首用已完成兜底。
        var next = Execute(previous, inventory, entryId);
        state.Tails[inventory] = next; // 替换旧值，成为新的队尾
        return next; // 原调用方 await 这笔排队任务
    }
    
    /// <summary>链上节点。上一笔失败时不继续消费旧货位，让错误保留在购买/切层任务中。</summary>
    private static async Task<bool> Execute(Task previous, MerchantInventory inventory, uint entryId)
    {
        await previous; // 等上一笔（含其嵌套赠品）彻底完成。若 previous 故障，此处失败沿链传播（毒化），把错误留在购买/切层任务里显式暴露。
        return await inventory.TryPurchaseEntryWithIdUnsynchronized(entryId); // 原 PurchaseEntry 的逻辑。
    }
    
    /// <summary>离店排水</summary>
    public static async Task DrainForExit(MerchantSynchronizer sync)
    {
        var state = Queues.GetOrCreateValue(sync);
        state.Closing = true; // 拒绝此后本地的新购买任务
        // 保留原版离店时跳过未选奖励的语义，随后等赠品及排队购买真正落账。
        RunManager.Instance.RewardsSetSynchronizer.BeforeLeavingRoom();
        Task[] pending;
        do
        {
            pending = state.Tails.Values.ToArray();
            await Task.WhenAll(pending).WaitAsync(TimeSpan.FromSeconds(30)); // 等这批全部完成，30 秒上限。
        } while (!pending.SequenceEqual(state.Tails.Values)); // 若排水期间有网络购买，导致尾任务变化，就再等一轮，直到序列稳定。
    }
    
    internal static void Reset(MerchantSynchronizer sync) => Queues.Remove(sync);
}