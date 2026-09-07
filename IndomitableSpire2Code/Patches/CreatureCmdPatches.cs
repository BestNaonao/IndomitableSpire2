using HarmonyLib;
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
/// 不补丁 Creature.LoseBlockInternal，避免把战斗结束等底层清理误报成 LoseBlock 指令。
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.LoseBlock))]
public static class CreatureCmdLoseBlockPatch
{
    private sealed record PatchState(ICombatState? CombatState, int ActualLoss, bool IsSuppressed);
    
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Creature target, decimal amount, out PatchState __state)
    {
        var isSuppressed = BlockLossHookSuppression.ConsumeForCurrentCommand();
        var canLoseBlock = !CombatManager.Instance.IsOverOrEnding && !target.IsDead && amount > 0;
        var actualLoss = canLoseBlock ? CalculateActualLoss(target.Block, amount) : 0;
        __state = new PatchState(target.CombatState, actualLoss, isSuppressed);
    }
    
    [HarmonyPostfix]
    private static void Postfix(
        PlayerChoiceContext choiceContext, Creature target, Creature? remover, PatchState __state, ref Task __result)
    {
        if (__state.IsSuppressed || __state.ActualLoss <= 0) return;
        __result = DispatchAfterOriginal(__result, __state.CombatState, choiceContext, target, __state.ActualLoss, remover);
    }
    
    /// <summary>
    /// 精确复现 LoseBlockInternal 的整数转换和零下限，避免把请求量误当成实际损失量。
    /// </summary>
    private static int CalculateActualLoss(int blockBefore, decimal requestedAmount) => 
        blockBefore - (int)Math.Max(blockBefore - requestedAmount, 0);
    
    private static async Task DispatchAfterOriginal(Task originalTask, 
        ICombatState? combatState, PlayerChoiceContext choiceContext, Creature target, int actualLoss, Creature? remover)
    {
        // 先保持原版顺序：若格挡归零，AfterBlockBroken 会先完整结算。
        await originalTask;
        await CustomHook.AfterBlockLost(combatState, choiceContext, target, actualLoss, remover);
    }
}
