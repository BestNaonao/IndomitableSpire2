using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class RepairCmd
{
    /// <summary>
    /// 执行分布式的维修逻辑
    /// </summary>
    /// <param name="player">玩家实例</param>
    /// <param name="totalHeal">总恢复池数值</param>
    /// <param name="skipVisuals"></param>
    public static async Task RandomRepair(Player player, int totalHeal, bool skipVisuals=false)
    {
        if (totalHeal <= 0) return;
        
        // 1. 扫描手牌和抽牌堆中所有不满耐久的舰载机
        var targets = new List<CarrierAircraftCard>();
        targets.AddRange(PileType.Hand.GetPile(player).Cards.OfType<CarrierAircraftCard>().Where(c => !c.IsFullDurability()));
        targets.AddRange(PileType.Draw.GetPile(player).Cards.OfType<CarrierAircraftCard>().Where(c => !c.IsFullDurability()));

        if (targets.Count == 0) return;

        // 2. 随机抽取一个目标（使用战斗随机数种子）
        var target = player.RunState.Rng.CombatCardSelection.NextItem(targets);
        
        // 3. 计算该目标需要的恢复量
        var deficiency = (int)(target!.DynamicVars.MaxDurability().BaseValue - target.DynamicVars.Durability().BaseValue);
        var healToApply = Math.Min(deficiency, totalHeal);

        // 4. 应用恢复并播放预览特效
        target.Repair(healToApply);
        if (!skipVisuals)
            CardCmd.Preview(target);

        // 5. 递归：如果还有剩余恢复量，继续寻找下一个目标
        if (totalHeal - healToApply is var remainingHeal and > 0 )
        {
            await RandomRepair(player, remainingHeal);
        }
    }
}