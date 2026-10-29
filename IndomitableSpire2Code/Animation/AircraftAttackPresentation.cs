using System.Runtime.CompilerServices;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Random;

namespace IndomitableSpire2.IndomitableSpire2Code.Animation;

/// <summary>显式执行扩展的实现；普通攻击使用每次调用的局部状态，只有随机攻击需要登记目标适配。</summary>
internal static class AircraftAttackPresentation
{
    /// <summary>内部记录类，仅在使用随机攻击指令时作登记</summary>
    private sealed class RandomFlight
    {
        public IReadOnlyList<Creature> Targets { get; set; } = [];
        public Creature? RandomTarget { get; set; }
    }
    
    private static readonly ConditionalWeakTable<AttackCommand, RandomFlight> RandomFlights = new();
    // 字段引用委托仅初始化一次；执行时直接读写字段引用，不重复进行反射查找。
    private static readonly AccessTools.FieldRef<AttackCommand, Func<Task>?> Callback =
        AccessTools.FieldRefAccess<AttackCommand, Func<Task>?>("_afterAttackerAnim");
    private static readonly AccessTools.FieldRef<AttackCommand, bool> AllowDuplicates =
        AccessTools.FieldRefAccess<AttackCommand, bool>("_doesRandomTargetingAllowDuplicates");
    private static readonly Func<AttackCommand, IReadOnlyList<Creature>> OriginalTargets =
        AccessTools.MethodDelegate<Func<AttackCommand, IReadOnlyList<Creature>>>(
            AccessTools.Method(typeof(AttackCommand), "GetPossibleTargets"));
    
    internal static async Task<AttackCommand> Execute(AttackCommand command, PlayerChoiceContext? choiceContext)
    {
        if (command.ModelSource is not CarrierAircraftCard card)
            throw new ArgumentException("ExecuteWithAircraftFlight requires a CarrierAircraftCard source. Call FromCard first.", nameof(command));
        
        // AfterAttackerAnim 不限于视觉操作，不能丢弃调用方已经安装的异步回调。
        var originalCallback = Callback(command);
        var randomFlight = command.IsRandomlyTargeted ? new RandomFlight() : null;
        if (randomFlight != null) RandomFlights.Add(command, randomFlight);
        try
        {
            var played = false;
            // 安装发生在最终执行入口，避免后续 builder 的 AfterAttackerAnim 把演出覆盖。
            // 显式舰载机命令保持禁用原生人物动作，由回调统一播放一次 Attack，不保存/恢复动作开关。
            // 不使用 OnlyPlayAnimOnce，保留已有的每段 attacker VFX/SFX/回调。
            command.WithNoAttackerAnim().AfterAttackerAnim(async () =>
            {
                if (!played)
                {
                    played = true;
                    // 延迟到 BeforeAttack/段数修正之后再读取目标，不缓存构造命令时的战场快照。
                    var targets = randomFlight?.Targets ?? OriginalTargets(command).Where(c => c.IsAlive).ToArray();
                    await card.PlayAircraftAttackPresentation(targets);
                }
                if (originalCallback != null) await originalCallback();
            });
            // 完整复用原版 Execute 的伤害、次数修正、命中特效与攻击钩子。
            return await command.Execute(choiceContext);
        }
        finally
        {
            // 清理目标状态: 卸下临时演出包装，避免命令继续持有本次 played、母卡和随机目标闭包。这只清理回调，不恢复成可复用的攻击模板。
            Callback(command) = originalCallback;
            if (randomFlight != null) RandomFlights.Remove(command);
        }
    }
    
    internal static IReadOnlyList<Creature> GetTargets(AttackCommand command)
    {
        var targets = OriginalTargets(command);
        // 非随机命令、未显式接入的随机命令直接返回原候选集合，不改变其选取时序。
        if (!command.IsRandomlyTargeted || !RandomFlights.TryGetValue(command, out var state)) return targets;
        var valid = targets.Where(c => c.IsAlive).ToList();
        state.RandomTarget = null;
        if (valid.Count > 0)
        {
            if (!AllowDuplicates(command))
            {
                var previous = command.Results.SelectMany(r => r)
                    .Select(r => r.Receiver)
                    .ToHashSet();
                valid.RemoveAll(previous.Contains);
                if (valid.Count == 0)
                    throw new InvalidOperationException("No valid targets for attack with duplicates disallowed.");
            }
            // 原版在 AfterAttackerAnim 和 HitSfx 之后才抽目标；这里只把同一次抽取提前，实际计算逻辑相同。
            // 每段仍恰好消耗一次 CombatTargets RNG，关闭飞行、缺模型时也走同一逻辑。
            var attacker = command.Attacker!;
            state.RandomTarget = (attacker.Player ?? attacker.PetOwner)!.RunState.Rng.CombatTargets.NextItem(valid)!;
            state.Targets = [state.RandomTarget];
            return state.Targets;
        }
        state.Targets = valid;
        return targets;
    }
    
    internal static Creature? ConsumeRandomTarget(Rng rng, IEnumerable<Creature> candidates, AttackCommand command) =>
        command.IsRandomlyTargeted && RandomFlights.TryGetValue(command, out var state) && state.RandomTarget != null
            ? state.RandomTarget : rng.NextItem(candidates);
}