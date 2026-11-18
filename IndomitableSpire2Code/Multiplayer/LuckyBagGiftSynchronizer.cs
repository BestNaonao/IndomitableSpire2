using System.Runtime.CompilerServices;
using BaseLib.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Multiplayer;

/// <summary>外层类协调各端抽取与提交，用"同规则 + 同 RNG 重放 + ID 比对"保证各端结果一致</summary>
public static class LuckyBagGiftSynchronizer
{
    private sealed class GiftState
    {
        public readonly TaskCompletionSource<LuckyBagGiftMessage> Message = new(); // 服务收信方，Receive 完成，仅远端使用。
        public Task? Execution; // 服务执行方，缓存已启动的结算任务，由 Give 启动，双端都执行。
    }
    
    // Inventory 的生命周期就是一次商店；重连/新局创建新实例，不沿用静态赠品编号。
    private static readonly ConditionalWeakTable<MerchantInventory, GiftState> Gifts = new();
    
    /// <summary>福袋给予赠品的幂等入口</summary>
    public static Task Give(MerchantInventory inventory, MerchantRoom room)
    {
        var state = Gifts.GetOrCreateValue(inventory); // 可能 Receive 已经先创建，就不重复创建
        return state.Execution ??= Execute(inventory, room, state); // 返回缓存的结算任务，没有任务时创建一个任务
    }
    
    /// <summary>福袋给予赠品的收信闸门</summary>
    internal static void Receive(LuckyBagGiftMessage message, ulong senderId)
    {
        var run = RunManager.Instance;
        // 位置校验 + 状态与房间校验 + GUID 格式校验 + 枚举定义校验。原版位置缓冲还会转发已访问楼层的旧消息，必须额外拒绝过期操作。
        if (message.Location != run.RunLocationTargetedBuffer.CurrentLocation ||
            run.DebugOnlyGetState() is not { CurrentRoom: MerchantRoom } state ||
            !Guid.TryParseExact(message.OperationId, "N", out _) || !Enum.IsDefined(message.Rule)) return;
        var player = state.Players.FirstOrDefault(p => p.NetId == senderId); // 按 NetId 寻找玩家
        if (player == null || LocalContext.IsMe(player)) return; // 未找到或者是本地，则丢弃消息。
        var inventory = run.MerchantSynchronizer.GetInventoryFor(player); // 取玩家对应的库存
        // 允许消息先于购买钩子到达。每个库存只有一个完成源；重投不会再次抽取/入包。
        Gifts.GetOrCreateValue(inventory).Message.TrySetResult(message);
    }
    
    /// <summary>双路径结算（每库存恰好一次）</summary>
    private static async Task Execute(MerchantInventory inventory, MerchantRoom room, GiftState state)
    {
        var player = inventory.Player;
        var run = RunManager.Instance;
        var service = run.NetService;
        var location = run.RunLocationTargetedBuffer.CurrentLocation;
        LuckyBagGiftMessage message;
        (MerchantRelicEntry? Entry, RelicModel Relic) selection; // 命名元组
        if (LocalContext.IsMe(player)) // 本地玩家，抽取 + 广播
        {
            var rule = await LuckyBagRuleSynchronizer.GetRule(); // 同步房主的规则
            if (player.RunState.CurrentRoom != room || player.Creature.IsDead)
                throw new InvalidOperationException("福袋赠品结算前已离开商店或玩家已死亡。");
            selection = Select(inventory, rule); // 确定性抽取
            message = new LuckyBagGiftMessage
            {
                OperationId = Guid.NewGuid().ToString("N"), Location = location,
                Rule = rule, EntryId = selection.Entry?.Id, RelicId = selection.Relic.Id
            };
            if (service.Type is NetGameType.Host or NetGameType.Client) // 非单人模式下进行送信
                CustomTargetedMessageWrapper.Send(message, service);
        }
        else // 远端玩家，等信 + 重放校验
        {
            void Disconnected(NetErrorInfo _) => state.Message.TrySetCanceled();
            service.Disconnected += Disconnected; // 订阅掉线事件，掉线自动取消
            try
            {
                message = await state.Message.Task.WaitAsync(TimeSpan.FromSeconds(30)); // 等待收信触发 Receive 获得结果。
            }
            finally
            {
                service.Disconnected -= Disconnected; // 无论成功与否，退订以防泄露。
            }
            if (player.RunState.CurrentRoom != room || message.Location != location)
                throw new InvalidOperationException("福袋赠品操作与当前商店不匹配。");
            // 按收信的规则重放抽取，并比较收信的条目和遗物 Id 和抽取结果，校验一致性。
            selection = Select(inventory, message.Rule);
            if (selection.Entry?.Id != message.EntryId || selection.Relic.Id != message.RelicId)
                throw new InvalidOperationException($"福袋赠品抽取不一致：操作 {message.OperationId}，" +
                    $"本地 {selection.Entry?.Id}/{selection.Relic.Id}，远端 {message.EntryId}/{message.RelicId}。");
        }
        // 共同提交
        if (selection.Entry is { } entry) // 非空意味着有库存，走自定义给予路线
            await CustomMerchantCmd.GiveRelic(inventory, entry, message.Rule);
        else if (message.Rule == LuckyBagRelicRule.Direct) // 直接获得，否则走赠与路线
            await RelicCmd.Obtain(selection.Relic, player);
        else
            await CustomRelicCmd.OfferGift(selection.Relic, player, message.Rule, fromPool: true);
    }
    
    /// <summary>确定性抽取。根据存货和规则决定。现在将抽取和给予/获得/赠与分为两个步骤。</summary>
    private static (MerchantRelicEntry? Entry, RelicModel Relic) Select(MerchantInventory inventory, LuckyBagRelicRule rule)
    {
        var player = inventory.Player;
        var stocked = inventory.RelicEntries.Where(e => e.IsStocked).ToList();
        if (stocked.Count == 0)
            return (null, rule == LuckyBagRelicRule.Direct
                ? RelicFactory.PullNextRelicForRewardFromFront(player).ToMutable()
                : CustomRelicCmd.PreviewFromPool(player));
        var entry = stocked[player.PlayerRng.Shops.NextInt(stocked.Count)];
        return entry.Id == null
            ? throw new InvalidOperationException("福袋赠品货位尚未分配同步 ID。")
            : (entry, entry.Model!);
    }
}