using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class RepairCmd
{
    /// <summary>
    /// 执行分布式的维修逻辑
    /// </summary>
    /// <param name="player">玩家实例</param>
    /// /// <param name="candidateCards">【解耦】外部提供的维修候选池</param>
    /// <param name="totalHeal">总恢复池数值</param>
    /// <param name="skipVisuals"></param>
    public static async Task RandomRepair(Player player, IEnumerable<CardModel> candidateCards, int totalHeal, bool skipVisuals = false)
    {
        // 缓存为列表，避免多次遍历 IEnumerable
        var candidatesList = candidateCards.ToList();
        while (totalHeal > 0) 
        {
            // 1. 在每一步循环中，动态过滤出池子里“仍不满耐久”的牌
            var targets = candidatesList.Where(c => !c.IsFullDurability()).ToList();
            if (targets.Count == 0) return; // 全修满了，或者没牌可修，直接结束
            
            // 2. 随机抽取一个目标（使用战斗随机数种子）
            var target = player.RunState.Rng.CombatCardSelection.NextItem(targets);
            
            // 3. 计算该目标需要的恢复量
            var deficiency = (int)(target!.DynamicVars.MaxDurability().BaseValue - target.DynamicVars.Durability().BaseValue);
            var healToApply = Math.Min(deficiency, totalHeal);
            
            // 4. 提取为通用方法调用：维修并且展示变更
            await ApplyRepairWithVisuals(target, healToApply, skipVisuals);
            
            // 5. 更新剩余恢复量，循环条件自动判断是否继续
            totalHeal -= healToApply;
        }
    }
    
    /// <summary>
    /// 将指定卡牌完全修满（带有视觉展示）
    /// </summary>
    public static async Task FullyRepair(CardModel card, bool skipVisuals = false)
    {
        // 如果已经满耐久，或者根本没有耐久机制，直接跳过
        if (card.IsFullDurability()) return;
        await ApplyRepairWithVisuals(
            card,
            card.DynamicVars.MaxDurability().IntValue - card.DynamicVars.Durability().IntValue,
            skipVisuals);
    }
    
    /// <summary>
    /// 内部核心方法：处理维修动画与实际数据修改
    /// </summary>
    private static async Task ApplyRepairWithVisuals(CardModel card, int healToApply, bool skipVisuals)
    {
        // 1. 表现层展示：修改预测值并闪烁
        if (!skipVisuals && card.DynamicVars.Durability() is DurabilityVar durVar)
        {
            durVar.PredictedHeal = healToApply;
            durVar.PreviewValue = durVar.BaseValue + healToApply;
            
            CardCmd.Preview(card); // 弹出卡牌进行展示
            
            await Cmd.CustomScaledWait(0.2f, 0.5f); // 等待动画播放
            
            durVar.PredictedHeal = 0;
            durVar.PreviewValue = 0;
        }
        
        // 2. 实际逻辑层：恢复耐久
        card.Repair(healToApply);
    }
}