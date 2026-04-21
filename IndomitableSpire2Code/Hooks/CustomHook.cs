using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;

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
}