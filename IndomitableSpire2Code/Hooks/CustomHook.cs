using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Hooks;

public static class CustomHook
{
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
        CombatState? combatState, 
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
        CombatState? combatState, 
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
}