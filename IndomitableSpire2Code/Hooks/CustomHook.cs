using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Hooks;

public static class CustomHook
{
    /// <summary>
    /// 广播 CreatureCmd.LoseBlock 实际造成的格挡损失。
    /// 与原版 AfterBlockBroken 一样直接使用战斗监听器快照，并逐个等待监听器完成。
    /// </summary>
    public static async Task AfterBlockLost(
        ICombatState? combatState,
        PlayerChoiceContext choiceContext,
        Creature target,
        int amount,
        Creature? remover,
        BlockLossReason reason)
    {
        if (combatState == null || amount <= 0) return;
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is IAfterBlockLostSubscriber subscriber && subscriber.ShouldReceiveAfterBlockLost(reason))
            {
                await subscriber.AfterBlockLost(choiceContext, target, amount, remover, reason);
                model.InvokeExecutionFinished();
            }
        }
    }
    
    public static async Task AfterEnergyGained(Player player, decimal amount)
    {
        if (player.Creature.CombatState == null) return;
        // 使用原版的 IterateHookListeners 来遍历当前战斗中所有合法的监听器模型
        foreach (var model in player.Creature.CombatState.IterateHookListeners())
        {
            if (model is IAfterEnergyGainedSubscriber subscriber)
            {
                // 等待当前模型的异步钩子执行完毕
                await subscriber.AfterEnergyGained(player, amount);
                
                // 【核心细节】：必须调用 InvokeExecutionFinished()，这是 STS2 引擎内部管理状态机的必要步骤
                model.InvokeExecutionFinished();
            }
        }
    }
    
    /// <summary>
    /// 阶段 1：尝试修改耐久损失，并收集成功实施修改的模型
    /// </summary>
    public static int ModifyDurabilityLossInCombat(
        ICombatState? combatState, 
        CardModel card, 
        int originalLoss, 
        out List<AbstractModel> modifyingModels)
    {
        modifyingModels = [];
        if (combatState == null || originalLoss <= 0) return originalLoss;
        var modifiedLoss = originalLoss;
        // 遍历监听模型列表，如果该模型成功修改了数据，将其加入生效名单
        foreach (var listener in combatState.IterateHookListeners())
        {
            if (listener is IDurabilityLossModifier modifier)
            {
                if (modifier.TryModifyDurabilityLoss(card, modifiedLoss, out modifiedLoss))
                {
                    modifyingModels.Add(listener);
                }
            }
        }
        return modifiedLoss;
    }
    
    /// <summary>
    /// 阶段 2：在战斗中实际造成了耐久修改后，通知生效名单内的模型（触发扣层数等逻辑）
    /// </summary>
    public static async Task AfterModifyingDurabilityLossInCombat(
        ICombatState? combatState, 
        CardModel card, 
        IEnumerable<AbstractModel> modifyingModels)
    {
        if (combatState == null) return;
        foreach (var model in modifyingModels)
        {
            if (model is IDurabilityLossModifier modifier)
            {
                await modifier.AfterModifyingDurabilityLoss(card);
                // 【核心细节】：必须通知底层引擎当前监听器的异步方法已执行完毕
                model.InvokeExecutionFinished();
            }
        }
    }
    
    /// <summary>
    /// 触发全局资源溢出事件的方法
    /// </summary>
    public static async Task AfterResourceOverflowed(
        PlayerChoiceContext choiceContext,
        Creature target,
        AbstractModel sourceModel,
        decimal overflowAmount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (target.CombatState == null || overflowAmount <= 0) return;
        // 遍历当前战斗中所有的合法监听器（包括手牌、遗物、能力等）
        foreach (var model in target.CombatState.IterateHookListeners())
        {
            if (model is IOnResourceOverflowSubscriber subscriber)
            {
                // 等待监听器处理溢出逻辑
                await subscriber.AfterResourceOverflowed(
                    choiceContext, target, sourceModel, overflowAmount, applier, cardSource);
                
                // 【核心细节】：通知引擎状态机
                model.InvokeExecutionFinished();
            }
        }
    }
    
    // 护盾破碎全局广播
    public static async Task AfterShieldBroken(Creature target)
    {
        if (target.CombatState == null) return;
        // 遍历当前战斗中所有的合法监听器（如内层装甲能力）
        foreach (var model in target.CombatState.IterateHookListeners())
        {
            if (model is IAfterShieldBrokenSubscriber subscriber)
            {
                await subscriber.AfterShieldBroken(target);
                model.InvokeExecutionFinished();
            }
        }
    }
    
    // 新增：生物逃跑全局广播
    public static async Task AfterCreatureEscaped(Creature escapedCreature, ICombatState combatState)
    {
        // 使用传入的战斗状态遍历监听器
        foreach (var model in combatState.IterateHookListeners())
        {
            if (model is IAfterCreatureEscapedSubscriber subscriber)
            {
                await subscriber.AfterCreatureEscaped(escapedCreature);
                model.InvokeExecutionFinished(); 
            }
        }
    }
}