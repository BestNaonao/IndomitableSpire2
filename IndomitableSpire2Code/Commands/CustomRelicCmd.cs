using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using IndomitableSpire2.IndomitableSpire2Code.Multiplayer;
using IndomitableSpire2.IndomitableSpire2Code.Rewards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

/// <summary>
/// 赠品遗物的通用处理：从池中预览、展示赠品选择、然后提交领取或销毁。
/// 商店赠品由 <see cref="CustomMerchantCmd"/> 提供已确定的货位遗物和交易回调；
/// 池赠品由 <see cref="GiveFromPool"/> 先按原版规则预览一个遗物。
/// </summary>
public static class CustomRelicCmd
{
    // 通过 Harmony 读取原版 RelicGrabBag 的私有状态，只用于构造临时副本；真正的池状态要等玩家选中或选择销毁时才修改。
    // 字段名来自原版 RelicGrabBag。这是对原版内部实现的依赖；游戏更新若改名或改池结构，需要同步检查这里。
    private static readonly AccessTools.FieldRef<RelicGrabBag, Dictionary<RelicRarity, List<RelicModel>>> Deques =
        AccessTools.FieldRefAccess<RelicGrabBag, Dictionary<RelicRarity, List<RelicModel>>>("_deques");
    private static readonly AccessTools.FieldRef<RelicGrabBag, List<RelicModel>> Fallback =
        AccessTools.FieldRefAccess<RelicGrabBag, List<RelicModel>>("_mpFallbackDequeue");
    private static readonly AccessTools.FieldRef<RelicGrabBag, bool> RefreshAllowed =
        AccessTools.FieldRefAccess<RelicGrabBag, bool>("_refreshAllowed");
    private static readonly AccessTools.FieldRef<RelicGrabBag, List<RelicModel>?> OriginalRelics =
        AccessTools.FieldRefAccess<RelicGrabBag, List<RelicModel>?>("_originalRelics");
    
    /// <summary>
    /// 按原版的前端遗物奖励规则预览一个遗物，但不从真实遗物池取走它。
    /// </summary>
    /// <remarks>
    /// 原版 <c>RelicFactory.PullNextRelicFromFront</c> 会依次决定稀有度、调用 <c>RelicGrabBag.PullFromFront</c>，
    /// 并从共享池移除结果。这里保留前两步的原版规则，但把 PullFromFront 放在临时副本上，所以玩家点“放回”时不必回滚真实池。
    /// 稀有度骰子和测试用遗物覆盖值仍只消耗一次，和原版执行一次抽取相同。
    /// </remarks>
    public static RelicModel PreviewFromPool(Player player)
    {
        var source = player.RelicGrabBag;
        // 复制现有的 RelicGrabBag，包括其 refresh 标记、稀有度队列、多人共享宝箱留下的备用队列、用于队列耗尽后刷新的原始清单等
        // 所有会影响 PullFromFront 结果的状态，用以创建新的 bag 对象。PullFromFront 对副本的删除/过滤/刷新不会改动 source。
        var preview = new RelicGrabBag(RefreshAllowed(source));
        foreach (var (rarity, relics) in Deques(source)) Deques(preview).Add(rarity, [..relics]);
        Fallback(preview).AddRange(Fallback(source));
        OriginalRelics(preview) = OriginalRelics(source)?.ToList();
        // 参考 RelicFactory.PullNextRelicFromFront(player) ，先 roll 稀有度，再用 preview 副本生成，
        // 既不会从玩家的实际 RelicGrabBag 的 availableDeque 中移除，也不会从 SharedRelicGrabBag 中移除，
        // RelicReward 要使用可变实例。转换只准备奖励显示对象，并不代表已获取遗物。
        var rarityToRoll = RelicFactory.RollRarity(player);
        return (TestRngInjector.ConsumeRelicOverride()
                ?? preview.PullFromFront(rarityToRoll, player.RunState)
                ?? RelicFactory.FallbackRelic).ToMutable();
    }
    
    /// <summary>
    /// 玩家确认一件预览自遗物池的赠品后，才把这次抽取提交到真实池。
    /// </summary>
    public static void ConsumePoolDraw(RelicModel relic, Player player)
    {
        // PullFromFront 的语义是从玩家队列取走一份，因此普通队列或多人备用队列只删一份。
        // 若非叠加遗物，后续 RelicCmd.Obtain/Discard 会像原版一样清除同 ID 的剩余副本。
        var candidates = Deques(player.RelicGrabBag).Values.Append(Fallback(player.RelicGrabBag));
        foreach (var deque in candidates)
        {
            var index = deque.FindIndex(candidate => candidate.Id == relic.Id);
            if (index < 0) continue;
            deque.RemoveAt(index);
            break;
        }
        // 对应原版 RelicFactory.PullNextRelicFromFront 在抽取后执行的共享池 Remove。
        RemoveFromBag(player.RunState.SharedRelicGrabBag, relic);
    }
    
    /// <summary>
    /// 从普通队列和多人备用队列中清除该 ID 的所有副本。
    /// </summary>
    private static void RemoveFromBag(RelicGrabBag bag, RelicModel relic)
    {
        // 原版 RelicGrabBag.Remove(relic) 会清除普通稀有度队列里的全部同 ID 遗物。
        bag.Remove(relic);
        // 原版 Remove 没有处理 _mpFallbackDequeue；获取/销毁后也清掉备用队列，避免这件遗物之后又从多人宝箱遗留队列里出现。
        Fallback(bag).RemoveAll(candidate => candidate.Id == relic.Id);
    }
    
    /// <summary>
    /// 只登记“看到了但没有选”的历史，不改遗物池或商店货位。
    /// </summary>
    /// <remarks>写入位置和字段对应原版 <c>RelicReward.OnSkipped</c>。</remarks>
    public static void RecordSkipped(RelicModel relic, Player player) =>
        player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).RelicChoices.Add(
            new ModelChoiceHistoryEntry(relic.Id, wasPicked: false));
    
    /// <summary>
    /// 实现“跳过并销毁”：提交池消耗和历史记录，但不真正把遗物放进背包。
    /// </summary>
    /// <remarks>
    /// 这里复制了原版 <c>RelicCmd.Obtain</c> 中与记录、遗物池和已见状态有关的部分，
    /// 并把选择记录标为未拾取。刻意不调用 <c>Player.AddRelicInternal</c>、
    /// <c>RelicModel.AfterObtained</c>、获取动画/音效或获取同步消息；这些步骤代表真正获得。
    /// 可叠加遗物遵循原版 Obtain 的池规则：不清除玩家池中剩余的同 ID 副本。
    /// </remarks>
    public static void Discard(RelicModel relic, Player player)
    {
        relic.AssertMutable();
        RecordSkipped(relic, player);
        if (!relic.IsStackable)
        {
            // 与原版 RelicCmd.Obtain 一样，非叠加遗物从个人池和共享池中彻底移除。
            // RemoveFromBag 还补上了原版 Remove 未覆盖的多人备用队列。
            RemoveFromBag(player.RelicGrabBag, relic);
            RemoveFromBag(player.RunState.SharedRelicGrabBag, relic);
        }
        // 原版 Obtain 会设置此楼层；即使没放进背包，遗物已在本次奖励中被决定并销毁。
        relic.FloorAddedToDeck = player.RunState.TotalFloor;
        // 与原版 Obtain 一样，仅本地玩家记为已见。不会触发遗物获得动画或 AfterObtained。
        if (LocalContext.IsMe(player)) SaveManager.Instance.MarkRelicAsSeen(relic);
    }
    
    /// <summary>
    /// 从遗物池赠送遗物。直接获取走原版完整路径；另外两种规则先展示可选奖励。
    /// </summary>
    public static async Task GiveFromPool(Player player, LuckyBagRelicRule rule)
    {
        if (rule == LuckyBagRelicRule.Direct)
        {
            // 直接获取逐步对应原版：RelicFactory 从池抽取并更新共享池，RelicCmd.Obtain
            // 入包并执行 AfterObtained，最后由原版 RewardSynchronizer 通知其他玩家。
            var relic = RelicFactory.PullNextRelicFromFront(player).ToMutable();
            await RelicCmd.Obtain(relic, player);
            RunManager.Instance.RewardSynchronizer.SyncLocalObtainedRelic(relic);
            return;
        }
        // 非直接模式先只在副本上抽取并显示。领取/销毁/放回都由 OfferGift 处理。
        await OfferGift(PreviewFromPool(player), player, rule, fromPool: true);
    }
    
    /// <summary>
    /// 打开一个只负责记录选择的原版奖励界面，并在界面关闭后提交领取、放回或销毁。
    /// </summary>
    /// <remarks>
    /// UI 来自 <c>RewardsCmd.OfferCustom</c>，其调用方式参考原版“小型扭蛋”的 AfterObtained；
    /// 标题由 GiftRewardsScreenPatch 调整，遗物图标/跳过按钮沿用 RelicReward。
    /// 传入的 GiftRelicReward 不在选择时立即获取，所以这里统一执行后续操作，避免商店回调和奖励回调各自再获取一次。
    /// 多人模式先广播同一个赠品，再让原版奖励同步器同步选择。
    /// </remarks>
    /// <param name="relic">已经确定并用于展示的遗物。</param>
    /// <param name="player">这份赠品对应的玩家。</param>
    /// <param name="rule">直接获取以外的放回/销毁规则。</param>
    /// <param name="fromPool">true 表示赠品先从随机池预览；false 表示它来自当前商店货位。</param>
    /// <param name="claim">商店领取回调；为空时用原版 RelicCmd.Obtain 直接领取。</param>
    /// <param name="discard">商店销毁回调，用于下架货位并按原版规则补货；池赠品不需要它。</param>
    /// <param name="synchronize">本地发起时广播赠品；网络消息接收端传 false，避免再次广播。</param>
    public static async Task OfferGift(RelicModel relic, Player player, LuckyBagRelicRule rule, bool fromPool,
        Func<Task<bool>>? claim = null, Func<Task>? discard = null, bool synchronize = true)
    {
        if (player.Creature.IsDead) return;
        // 商店交互最初只发生在一端。其他端需先拿到相同遗物和规则，再各自建立相同奖励集，
        // 后续的选择/跳过由原版 RewardsSetSynchronizer 按奖励集 ID 同步。
        if (synchronize) LuckyBagGiftMessage.Send(relic, rule, fromPool);
        // UI 由父类和补丁提供，GiftRelicReward 只记录决定，不调用基类获取/历史逻辑。
        var reward = new GiftRelicReward(relic, player);
        await RewardsCmd.OfferCustom(player, [reward]);
        if (reward.Accepted)
        {
            // 遗物池抽取只有在玩家确认后才提交，在各端更新镜像池；真正的背包获取只在本地端执行，由原版 RewardSynchronizer 发一次消息。
            if (fromPool) ConsumePoolDraw(relic, player);
            if (!LocalContext.IsMe(player)) return;
            if (claim != null)
            {
                // 商店遗物必须调用原版免费购买流程，以保留购买钩子、清理/补货和 UI 刷新。
                // 重查货位是乐观状态校验：等待选择期间，若货位已改变，就拒绝错误领取。
                if (!await claim()) throw new InvalidOperationException("赠品在选择期间已不在原商店货位。");
            }
            else
            {
                // 遗物池随机遗物没有商店交易回调，直接使用原版 Obtain 并同步一次获得消息。
                await RelicCmd.Obtain(relic, player);
                RunManager.Instance.RewardSynchronizer.SyncLocalObtainedRelic(relic);
            }
        }
        else if (reward.Skipped)
        {
            // 放回 = 仅记“未选”。不提交预览抽取，所以随机遗物仍留在池里；商店回调也不运行，所以原商品保留在原货位。
            if (rule == LuckyBagRelicRule.SkipAndReturn) RecordSkipped(relic, player);
            else
            {
                // 销毁 = 池赠品提交这次抽取；商店赠品则由 discard 回调执行货位下架/补货。其他端只镜像遗物池与历史状态。
                if (fromPool) ConsumePoolDraw(relic, player);
                if (LocalContext.IsMe(player) && discard != null) await discard();
                else Discard(relic, player);
            }
        }
    }
}