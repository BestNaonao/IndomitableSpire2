using System.Runtime.CompilerServices;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Registries;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Hooks;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// 用于传递计算结果的上下文
/// </summary>
public class BlockRetentionContext
{
    public int FinalRetainedAmount { get; set; }
    public List<AbstractModel> TriggeredModels { get; } = [];
}

/// <summary>
/// 自定义新钩子：成功保留格挡后触发
/// </summary>
public static class CustomBlockHooks
{
    // 我们利用字典在两个生命周期之间临时存储数据，以 Creature 为键的弱引用表，完美解决多人冲突和内存泄漏。
    public static readonly ConditionalWeakTable<Creature, BlockRetentionContext> ActiveContexts = new();

    public static async Task OnBlockSuccessfullyRetained(Creature creature, int retainedAmount)
    {
        // 在这里你可以遍历玩家的遗物、能力，触发 "当保留格挡时获得力量" 之类的新机制，留白供未来扩展
        await Task.CompletedTask; 
    }
}

[HarmonyPatch(typeof(Hook))]
public static class BlockRetentionPatches 
{
    [HarmonyPatch(nameof(Hook.ShouldClearBlock))]
    [HarmonyPrefix]
    public static bool ShouldClearBlock_Prefix(CombatState combatState, Creature creature, ref AbstractModel? preventer, ref bool __result) 
    {
        // 1. 收集所有原本返回 false 的对象
        var activeModels = combatState.IterateHookListeners().ToList()
            .Where(listener => !listener.ShouldClearBlock(creature)).ToList();
        
        if (activeModels.Count == 0)
        {
            // 没有对象阻止清空，走原版逻辑直接清空
            __result = true;
            preventer = null;
            return false; // 拦截原版，因为我们已经替它做了决定
        }
        
        // 2. 对比注册表进行数值计算
        var aggregateSum = 0;
        var nonAggregateValues = new List<int>();
        var context = new BlockRetentionContext();
        
        foreach (var model in activeModels)
        {
            if (BlockRetentionRegistry.TryGetProvider(model, out var provider))
            {
                var val = provider!.CalculateRetainedBlock(model, creature);
                if (provider.ShouldAggregate)
                    aggregateSum += val;
                else
                    nonAggregateValues.Add(val);
                
                context.TriggeredModels.Add(model);
            }
            else
            {
                // 兼容未注册的第三方 Mod（兜底按最大值全量保留处理）
                nonAggregateValues.Add(creature.Block);
                context.TriggeredModels.Add(model);
            }
        }
        
        // 3. 计算最终保留值 n 
        var finalRetained = aggregateSum; // 先把聚合的加成算上
        if (nonAggregateValues.Count > 0)
        {
            // 聚合值应该加在基准值上
            finalRetained += BlockRetentionRegistry.UseMaxForNonAggregates ? 
                nonAggregateValues.Max() : nonAggregateValues.Min();
        }
        
        // 保留的格挡不可能超过当前已有格挡，并将结果存入上下文
        context.FinalRetainedAmount = Math.Min(finalRetained, creature.Block);
        CustomBlockHooks.ActiveContexts.AddOrUpdate(creature, context);
        
        // 4. 提交给原版游戏：告诉它我们有 preventer，不需要清空
        __result = false;
        preventer = activeModels[0]; // 随便传第一个通过 Contains 检测的模型
        return false; 
    }
    
    [HarmonyPatch(nameof(Hook.AfterPreventingBlockClear))]
    [HarmonyPrefix]
    public static bool AfterPreventingBlockClear_Prefix(CombatState combatState, AbstractModel preventer, Creature creature, ref Task __result)
    {
        // 检查我们是否有接管当前状态的计算
        if (!CustomBlockHooks.ActiveContexts.TryGetValue(creature, out var context))
            return true; // 没有记录，放行给原版逻辑（虽然正常情况下不会发生）
        
        // 执行我们的自定义异步处理逻辑
        __result = HandleBlockRetentionAsync(combatState, preventer, creature, context);
        return false; // 拦截原版，防止原版的 SturdyClamp 等重复执行扣格挡逻辑
    }
    
    private static async Task HandleBlockRetentionAsync(CombatState combatState, AbstractModel originalPreventer, Creature creature, BlockRetentionContext context)
    {
        // 确保安全：如果不在监听器列表中直接退出
        if (!combatState.IterateHookListeners().Contains(originalPreventer))
        {
            CustomBlockHooks.ActiveContexts.Remove(creature);
            return;
        }
        
        try
        {
            var toLose = creature.Block - context.FinalRetainedAmount;
            
            // 1. 如果最终保留值小于当前格挡，扣除多余部分（如：15点格挡只保留10点，扣除5点）
            if (toLose > 0) await CreatureCmd.LoseBlock(creature, toLose);
            
            // 2. 触发对应模型的表现效果 (如闪烁)
            foreach (var model in context.TriggeredModels)
                if (BlockRetentionRegistry.TryGetProvider(model, out var provider))
                    provider!.OnRetentionTriggered(model, creature);
            
            // 3. 触发全新的扩展钩子
            await CustomBlockHooks.OnBlockSuccessfullyRetained(creature, context.FinalRetainedAmount);
        }
        finally
        {
            // 无论上方发生什么异常，确保令牌交还，并清除临时状态
            originalPreventer.InvokeExecutionFinished();
            CustomBlockHooks.ActiveContexts.Remove(creature);
        }
    }
}