using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Escape))]
public static class CreatureCmdEscapePatch
{
    // 在逃跑逻辑执行前触发，将即将丢失的 CombatState 抢救保存到 __state 中
    [HarmonyPrefix]
    public static void Prefix(Creature creature, out ICombatState? __state)
    {
        __state = creature.CombatState;
    }
    
    // 在逃跑逻辑执行后触发，接收 prefix 传过来的 __state
    [HarmonyPostfix]
    public static void Postfix(Creature creature, ICombatState? __state)
    {
        // 此时 creature.CombatState 虽然已经是 null 了，但我们有备份的 __state！
        if (__state != null) TaskHelper.RunSafely(CustomHook.AfterCreatureEscaped(creature, __state));
    }
}

/// <summary>
/// 在 CreatureCmd.LoseBlock 的命令边界广播实际格挡损失。
/// Internal 补丁只在此命令的同步调用栈内测量差值；广播仍严格限定在命令边界。
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.LoseBlock))]
public static class CreatureCmdLoseBlockPatch
{
    [ThreadStatic]
    private static PatchState? _activeCapture;
    
    private sealed class PatchState(ICombatState? combatState, Creature target, BlockLossReason reason, PatchState? previous)
    {
        public ICombatState? CombatState { get; } = combatState;
        public Creature Target { get; } = target;
        public BlockLossReason Reason { get; } = reason;
        public PatchState? Previous { get; } = previous;
        public int ActualLoss { get; private set; }
        
        public void RecordLoss(int blockBefore, int blockAfter)
        {
            var loss = Math.Max(blockBefore - blockAfter, 0);
            if (loss <= 0) return;
            // 若其他补丁在同一 LoseBlock 命令内额外调用 Internal，累计每次真实减少量。
            ActualLoss = (int)Math.Min(int.MaxValue, (long)ActualLoss + loss);
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(Creature target, out PatchState __state)
    {
        var reason = BlockLossHookContext.ConsumeReasonForCurrentCommand(target);
        __state = new PatchState(target.CombatState, target, reason, _activeCapture);
        _activeCapture = __state;
    }
    
    [HarmonyPostfix]
    private static void Postfix(
        PlayerChoiceContext choiceContext, Creature target, Creature? remover, PatchState? __state, ref Task __result)
    {
        // 其他 Harmony Prefix 可能跳过原方法和本 Prefix；Postfix 必须容忍空 state。
        if (__state == null) return;
        RestoreCapture(__state);
        if (__state.ActualLoss <= 0) return;
        __result = DispatchAfterOriginal(
            __result, __state.CombatState, choiceContext, target, __state.ActualLoss, remover, __state.Reason);
    }
    
    [HarmonyFinalizer]
    private static Exception? Finalizer(Exception? __exception, PatchState? __state)
    {
        // Postfix 因同步异常未执行时仍恢复捕获栈；正常路径下该操作是幂等的。
        RestoreCapture(__state);
        return __exception;
    }
    
    private static void RestoreCapture(PatchState? state)
    {
        if (state != null && ReferenceEquals(_activeCapture, state))
            _activeCapture = state.Previous;
    }
    
    internal static void RecordInternalLoss(Creature target, int blockBefore, int blockAfter)
    {
        var capture = _activeCapture;
        if (capture == null || !ReferenceEquals(capture.Target, target)) return;
        capture.RecordLoss(blockBefore, blockAfter);
    }
    
    private static async Task DispatchAfterOriginal(
        Task originalTask, ICombatState? combatState, PlayerChoiceContext choiceContext, Creature target,
        int actualLoss, Creature? remover, BlockLossReason reason)
    {
        // 先保持原版顺序：若格挡归零，AfterBlockBroken 会先完整结算。
        await originalTask;
        await CustomHook.AfterBlockLost(combatState, choiceContext, target, actualLoss, remover, reason);
    }
}

/// <summary>
/// 只在 LoseBlock 命令作用域内测量同步突变前后的真实差值；此处不执行也不广播任何钩子。
/// </summary>
[HarmonyPatch(typeof(Creature), nameof(Creature.LoseBlockInternal))]
public static class CreatureLoseBlockInternalMeasurementPatch
{
    [HarmonyPrefix]
    private static void Prefix(Creature __instance, out int __state)
    {
        __state = __instance.Block;
    }
    
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Creature __instance, int __state)
    {
        CreatureCmdLoseBlockPatch.RecordInternalLoss(__instance, __state, __instance.Block);
    }
}