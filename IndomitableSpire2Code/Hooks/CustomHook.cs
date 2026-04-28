using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
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

    public static async Task AfterDynamicVarAmountChanged(
        AbstractModel sourceModel,
        string variableName,
        decimal originalAmount,
        decimal offsetAmount,
        Creature target,
        Creature? applier)
    {
        if (target.CombatState == null) return;
        
        // 使用原版的 IterateHookListeners 来遍历当前战斗中所有合法的监听器模型
        foreach (var model in target.CombatState.IterateHookListeners())
        {
            if (model is IAfterDynamicVarAmountChangedSubscriber subscriber)
            {
                await subscriber.AfterDynamicVarAmountChanged(sourceModel, variableName, originalAmount, offsetAmount, target, applier);
                model.InvokeExecutionFinished();
            }
        }
    }
}