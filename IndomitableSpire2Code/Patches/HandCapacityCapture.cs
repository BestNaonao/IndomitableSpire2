using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// 补丁内部的命令记录。只保存原版分支事实，不调用订阅接口，也不保存能力层数或效果回调。
/// 以命令自己的结果列表为弱键，隔离嵌套操作；原任务失败时记录随结果对象回收。
/// </summary>
internal static class HandCapacityCapture
{
    private static readonly ConditionalWeakTable<object, PendingCommand> Pending = new();
    private static readonly AsyncLocal<DrawCheck?> CurrentDrawCheck = new();
    private static readonly Func<Player, bool> NativeCheck = AccessTools.MethodDelegate<Func<Player, bool>>(
        AccessTools.Method(typeof(CardPileCmd), "CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot"));
    
    /// <summary>在原版决定满手后、移除原牌前，记录被拒绝入手的尝试、原牌堆及回合信息。</summary>
    /// <param name="results">最终 Add 重载本次调用的结果列表，也是记录的唯一归属。</param>
    /// <param name="resultIndex">当前处理的结果索引，避免同牌堆回流或重复尝试被合并。</param>
    /// <param name="isFullHandAdd">原版及 BaseLib 修正后的实际容量判断，不自行重算上限。</param>
    internal static void CaptureHandAttempt(List<CardPileAddResult> results, int resultIndex, bool isFullHandAdd)
    {
        if (!isFullHandAdd) return;
        var result = results[resultIndex];
        if (CaptureScope(result.cardAdded.Owner) is not {} scope) return;
        Pending.GetOrCreateValue(results).HandAttempts[resultIndex] =
            new HandOverflow(scope, result.cardAdded, result.oldPile?.Type ?? PileType.None);
    }
    
    /// <summary>执行原插入操作，成功返回后确认一次因满手而未能入手，不以牌的最终位置反推原因。</summary>
    internal static void AddAndRecord(CardPile pile, CardModel card, int index, bool silent,
        List<CardPileAddResult> results, int resultIndex)
    {
        pile.AddInternal(card, index, silent);
        if (Pending.TryGetValue(results, out var pending) && pending.HandAttempts.Remove(resultIndex, out var attempt))
            pending.HandOverflows.Add(attempt);
    }
    
    /// <summary>观察原版计算出的剩余容量，返回原值，保持抽牌控制流不变。</summary>
    /// <param name="capacity">原版按当前实际手牌上限算出的剩余容量。</param>
    /// <param name="result">该 Draw 已执行的抽牌列表，同时用作本命令的记录键。</param>
    /// <param name="player">请求抽牌的玩家。</param>
    /// <param name="requested">原版已完成取整后的请求次数，不是剩余次数。</param>
    /// <param name="fromHandDraw">原命令的回合开始抽牌标记。</param>
    /// <param name="choiceContext">原命令上下文，后续广播继续使用同一实例。</param>
    internal static int RecordComputedCapacity(int capacity, List<CardModel> result, Player player,
        int requested, bool fromHandDraw, PlayerChoiceContext choiceContext)
    {
        if (capacity <= 0) RecordDraw(result, player, requested, fromHandDraw, choiceContext);
        return capacity;
    }
    
    /// <summary>观察洗牌后、实际移动前的直接满手比较，返回原判断结果。</summary>
    internal static bool RecordFullHandComparison(bool isFull, List<CardModel> result, Player player,
        int requested, bool fromHandDraw, PlayerChoiceContext choiceContext)
    {
        if (isFull) RecordDraw(result, player, requested, fromHandDraw, choiceContext);
        return isFull;
    }
    
    /// <summary>给同步可抽检查绑定所属 Draw；嵌套检查结束后恢复外层归属。</summary>
    internal static bool CheckDrawPossible(Player player, List<CardModel> result, int requested, bool fromHandDraw,
        PlayerChoiceContext choiceContext)
    {
        var previous = CurrentDrawCheck.Value;
        CurrentDrawCheck.Value = new DrawCheck(result, player, requested, fromHandDraw, choiceContext);
        try { return NativeCheck(player); }
        finally { CurrentDrawCheck.Value = previous; }
    }
    
    /// <summary>只由原版 HAND_FULL 分支调用，NO_DRAW 分支及独立可抽检查均不会产生记录。</summary>
    internal static void RecordFullHandCheck(Player player)
    {
        var context = CurrentDrawCheck.Value;
        if (context != null && context.Player == player)
            RecordDraw(context.Result, player, context.Requested, context.FromHandDraw, context.ChoiceContext);
    }
    
    /// <summary>
    /// 每个 Draw 只记录一次容量拒绝，并用拒绝时仍可抽取的牌数限制次数。
    /// 初始及抽牌后的容量计算都可能早于原版 NO_DRAW 检查，满手本身不能证明有牌被阻止抽取。
    /// </summary>
    /// <param name="result">本次命令已经抽取的牌；与 requested 的差值为尚未完成的请求数。</param>
    /// <param name="player">被拒绝抽牌的玩家，只检查其抽牌堆及可洗回的弃牌堆。</param>
    /// <param name="requested">原版取整后的总请求次数。</param>
    /// <param name="fromHandDraw">原命令的回合开始抽牌标记。</param>
    /// <param name="choiceContext">原抽牌命令的上下文，随有效记录交给后续钩子。</param>
    private static void RecordDraw(List<CardModel> result, Player player, int requested, bool fromHandDraw,
        PlayerChoiceContext choiceContext)
    {
        // 只有还有卡牌没被处理放入 result 时才记录
        if (requested - result.Count is var remaining && remaining <= 0) return;
        // 与原版可抽检查使用相同的两个牌堆；只在容量拒绝当下计数，不实际取牌或触发洗牌。
        // 已抽完最后一张时为零；请求超过存量时，无牌支持的那部分也不计为过量抽牌。
        var available = PileType.Draw.GetPile(player).Cards.Count + PileType.Discard.GetPile(player).Cards.Count;
        if ((Math.Min(remaining, available) is var count && count <= 0) || CaptureScope(player) is not {} scope) return;
        Pending.GetOrCreateValue(result).DrawOverflow ??= new DrawOverflow(scope, choiceContext, count, fromHandDraw);
    }
    
    /// <summary>先移除记录，再交给命令边界补丁广播，避免收益重入时重复消费。</summary>
    internal static IReadOnlyList<HandOverflow> TakeHandOverflows(object result)
    {
        if (!Pending.TryGetValue(result, out var pending)) return [];
        Pending.Remove(result);
        return pending.HandOverflows;
    }
    
    /// <summary>取出一次抽牌命令的拒绝记录；原任务抛异常时不会走到这个结算入口。</summary>
    internal static DrawOverflow? TakeDrawOverflow(object result)
    {
        if (!Pending.TryGetValue(result, out var pending)) return null;
        Pending.Remove(result);
        return pending.DrawOverflow;
    }
    
    private sealed class PendingCommand
    {
        internal Dictionary<int, HandOverflow> HandAttempts { get; } = [];
        internal List<HandOverflow> HandOverflows { get; } = [];
        internal DrawOverflow? DrawOverflow { get; set; }
    }
    
    internal sealed record HandOverflow(Scope Scope, CardModel Card, PileType OldPileType);
    internal sealed record DrawOverflow(Scope Scope, PlayerChoiceContext ChoiceContext, int Count, bool FromHandDraw);
    
    internal sealed record Scope(Player Player, ICombatState CombatState, int TurnNumber, PlayerTurnPhase Phase)
    {
        /// <summary>防止旧命令在跨战斗、跨回合或玩家死亡后继续发出通知。</summary>
        internal bool IsCurrent => CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding && 
                                   Player.Creature.IsAlive && Player.Creature.CombatState == CombatState && 
                                   Player.PlayerCombatState?.TurnNumber == TurnNumber;
    }
    
    private static Scope? CaptureScope(Player player) =>
        player.Creature.CombatState is { } combat && player.PlayerCombatState is { } state &&
        CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding && player.Creature.IsAlive
            ? new Scope(player, combat, state.TurnNumber, state.Phase)
            : null;
    
    private sealed record DrawCheck(List<CardModel> Result, Player Player, int Requested, bool FromHandDraw,
        PlayerChoiceContext ChoiceContext);
}