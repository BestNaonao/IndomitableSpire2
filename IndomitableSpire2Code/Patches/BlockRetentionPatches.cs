using System.Runtime.CompilerServices;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using IndomitableSpire2.IndomitableSpire2Code.Registries;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// 用于传递计算结果的上下文
/// </summary>
public sealed class BlockRetentionContext(ICombatState combatState, AbstractModel expectedPreventer)
{
    public ICombatState CombatState { get; } = combatState;
    public AbstractModel ExpectedPreventer { get; } = expectedPreventer;
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
    public static bool ShouldClearBlock_Prefix(
        ICombatState combatState, Creature creature, ref AbstractModel? preventer, ref bool __result)
    {
        // 清除未走到 AfterPreventingBlockClear 的旧查询结果，避免后续调用误用陈旧上下文。
        CustomBlockHooks.ActiveContexts.Remove(creature);
        // 原版 ShouldClearBlock 在战斗结束阶段不会派发监听器；此处必须保持相同守卫。
        if (CombatManager.Instance.IsOverOrEnding && !CombatManager.Instance.IsStarting)
            return true;
        
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
        var context = new BlockRetentionContext(combatState, activeModels[0]);
        
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
            }   // 聚合类型的值累加，非聚合类型的值另外存放
            else
            {
                nonAggregateValues.Add(creature.Block);
                context.TriggeredModels.Add(model);
            }   // 兼容未注册的第三方 Mod（兜底按最大值全量保留处理）
        }
        
        // 3. 计算最终保留值 n 
        var finalRetained = aggregateSum; // 先把聚合的加成算上
        if (nonAggregateValues.Count > 0)
        {
            // 聚合值应该加在基准值上
            finalRetained += BlockRetentionRegistry.UseMaxForNonAggregates 
                ? nonAggregateValues.Max() : nonAggregateValues.Min();
        }
        
        // 保留值始终限定在 [0, 当前格挡]，防止第三方 provider 的负值或累计溢出污染状态。
        context.FinalRetainedAmount = Math.Clamp(finalRetained, 0, creature.Block);
        CustomBlockHooks.ActiveContexts.Add(creature, context);
        
        // 4. 提交给原版游戏：告诉它我们有 preventer，不需要清空
        __result = false;
        preventer = context.ExpectedPreventer; // 随便传第一个通过 Contains 检测的模型
        return false; 
    }
    
    [HarmonyPatch(nameof(Hook.AfterPreventingBlockClear))]
    [HarmonyPrefix]
    public static bool AfterPreventingBlockClear_Prefix(
        ICombatState combatState, AbstractModel preventer, Creature creature, ref Task __result)
    {
        // 1. 全局状态检查：战斗结束时清理并放行
        if (CombatManager.Instance.IsOverOrEnding && !CombatManager.Instance.IsStarting)
        {
            CustomBlockHooks.ActiveContexts.Remove(creature);
            return true;
        }
        
        // 2. 存在性检查：无上下文记录时直接放行原版
        if (!CustomBlockHooks.ActiveContexts.TryGetValue(creature, out var context))
        {
            return true; 
        }
        
        // 3. 业务校验：上下文过期或不匹配时，清理脏数据并放行原版
        if (context.CombatState != combatState || context.ExpectedPreventer != preventer)
        {
            CustomBlockHooks.ActiveContexts.Remove(creature);
            return true;
        }
        
        // 4. 正常拦截：执行自定义异步逻辑
        __result = HandleBlockRetentionAsync(combatState, preventer, creature, context);
        return false; // 拦截原版，防止原版的 SturdyClamp 等重复执行扣格挡逻辑
    }
    
    private static async Task HandleBlockRetentionAsync(
        ICombatState combatState, AbstractModel originalPreventer, Creature creature, BlockRetentionContext context)
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
            if (toLose > 0)
            {
                // 这是回合开始时为了保留上限而进行的维护性裁剪，由监听器按来源决定是否接收。
                using (BlockLossHookContext.UseReasonForNext(creature, BlockLossReason.RetentionAdjustment))
                {
                    await CreatureCmd.LoseBlock(new ThrowingPlayerChoiceContext(), creature, toLose, null);
                }
            }
            
            // 2. 触发对应模型的表现效果 (如闪烁)
            var currentListeners = combatState.IterateHookListeners().ToHashSet();
            foreach (var model in context.TriggeredModels)
            {
                if (currentListeners.Contains(model) && BlockRetentionRegistry.TryGetProvider(model, out var provider))
                    await provider!.OnRetentionTriggered(model, creature);
            }
            
            // 3. 触发全新的扩展钩子
            await CustomBlockHooks.OnBlockSuccessfullyRetained(creature, Math.Max(creature.Block, 0));
        }
        finally
        {
            // 先清上下文；即使 ExecutionFinished 抛错，也不会把陈旧状态留给下一次清理。
            CustomBlockHooks.ActiveContexts.Remove(creature);
            originalPreventer.InvokeExecutionFinished();
        }
    }
}