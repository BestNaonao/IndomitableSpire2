using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

public sealed class RoyalBounty : IndomitableRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    
    // 底层私有字段
    private int _combatTotalEnemyMaxHp;
    // 【核心修复】：将简单的自动属性升级为带通知机制的属性
    private int CombatTotalEnemyMaxHp 
    { 
        get => _combatTotalEnemyMaxHp;
        set
        {
            AssertMutable();
            if (_combatTotalEnemyMaxHp == value) return;
            _combatTotalEnemyMaxHp = value;
            InvokeDisplayAmountChanged();   // 通知底层引擎 UI 刷新本遗物的显示数值
        }
    }
    
    // 用于展示和计算总赏金池的大小
    public override bool ShowCounter => true;
    public override int DisplayAmount => (int)(CombatTotalEnemyMaxHp * (DynamicVars["BountyRate"].BaseValue / 100m));
    
    // 定义赏金比例变量，方便后续平衡调整或 UI 渲染
    protected override IEnumerable<DynamicVar> CanonicalVars => [new("BountyRate", 10M)];
    
    // Todo: 现在无法处理开局无限血量的门扉和逃跑的草蜢、胖地精
    public override Task BeforeCombatStart()
    {
        // 1. 战斗开始时重置生命值记录（清除上一场战斗的残留，以及进场钩子带来的错误累加）
        CombatTotalEnemyMaxHp = 0;
        
        // 2. 效仿 FurCoat，直接抓取当前战斗中已加载的初始敌人并进行统计
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return Task.CompletedTask;
        // 遍历场上所有合法的敌方目标
        foreach (var creature in combatState.Enemies)
            CombatTotalEnemyMaxHp += creature.MaxHp;
        
        return Task.CompletedTask;
    }
    
    public override Task AfterCreatureAddedToCombat(Creature creature)
    {
        // 每当有敌人进场（包括开局和中途召唤的衍生物），累加其最大生命值
        if (creature.IsEnemy)
            CombatTotalEnemyMaxHp += creature.MaxHp;
        return Task.CompletedTask;
    }
    
    public override Task AfterCombatEnd(CombatRoom room)
    {
        // 计算本场战斗产生的总金币，并创建玩家列表的副本以供洗牌
        var totalGold = DisplayAmount;
        var shuffledPlayers = Owner.RunState.Players.ToList();
        if (totalGold > 0 && shuffledPlayers.Count > 0)
        {
            Flash();
            
            // 直接使用 Niche（杂项）随机池，自动保证联机同步！
            var rng = Owner.RunState.Rng.Niche;
            
            // 1. 打乱玩家的排序
            shuffledPlayers.FisherYatesShuffle(rng);
            
            // 2. 公平的“抢红包”分配算法
            var remainingGold = totalGold;
            var remainingPeople = shuffledPlayers.Count;
            foreach (var player in shuffledPlayers)
            {
                int goldForThisPlayer;
                
                if (remainingPeople == 1)
                {
                    goldForThisPlayer = remainingGold;  // 最后一个人拿走剩下的所有钱（兜底）
                }
                else
                {
                    // 核心红包公式：随机区间为 [0, (剩余金额 / 剩余人数) * 2]，保证所有人的分配期望值绝对一致！
                    var maxAllowed = remainingGold / remainingPeople * 2;
                    goldForThisPlayer = (int)(rng.NextUnsignedInt() % (uint)(maxAllowed + 1));
                    if (goldForThisPlayer == 0) goldForThisPlayer++;
                }
                
                if (goldForThisPlayer > 0)
                    room.AddExtraReward(player, new GoldReward(goldForThisPlayer, player));
                
                remainingGold -= goldForThisPlayer;
                remainingPeople--;
            }
        }
        
        CombatTotalEnemyMaxHp = 0;
        return Task.CompletedTask;
    }
}