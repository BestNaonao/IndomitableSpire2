using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

/// <summary>
/// ！！！需要测试：组装师，偷窃草蜢，地精佣兵，门扉缔造者，瀑布巨兽，千足虫，胧光兽、实验体！！！
/// </summary>
public sealed class RoyalBounty : IndomitableRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    
    // 【核心重构 1】：自定义数据结构，记录生物的悬赏状态
    private class BountyRecord
    {
        public int MaxHpSeen;       // 见过的最高最大生命值
        public int MaxHpRewarded;   // 已经兑现为赏金的最大生命值
    }
    
    // 内部记录器：追踪每个敌方实体的悬赏记录
    private readonly Dictionary<Creature, BountyRecord> _enemyMaxHpTracker = new();
    
    // 将简单的自动属性升级为带通知机制的属性
    private int _combatTotalEnemyMaxHp;
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
    
    // 核心提取：更新生物最高生命值记录的安全方法
    private void UpsertEnemyMaxHpTracker(Creature creature)
    {
        if (!creature.IsEnemy || creature.ShowsInfiniteHp) return;
        
        if (!_enemyMaxHpTracker.TryGetValue(creature, out var record))
        {
            record = new BountyRecord();
            _enemyMaxHpTracker[creature] = record;
        }
        // 更新历史最高最大生命值
        record.MaxHpSeen = Math.Max(record.MaxHpSeen, creature.MaxHp);
    }
    
    public override Task BeforeCombatStart()
    {
        // 1. 战斗开始时重置生命值记录
        CombatTotalEnemyMaxHp = 0;
        _enemyMaxHpTracker.Clear();
        
        // 2. 效仿 FurCoat，直接抓取当前战斗中已加载的初始敌人并进行统计
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return Task.CompletedTask;
        foreach (var creature in combatState.Enemies)
            UpsertEnemyMaxHpTracker(creature);
        
        return Task.CompletedTask;
    }
    
    public override Task AfterCreatureAddedToCombat(Creature creature)
    {
        // 每当有敌人进场（包括开局和中途召唤的衍生物），记录其最大生命值
        if (creature.IsEnemy)
            UpsertEnemyMaxHpTracker(creature);
        return Task.CompletedTask;
    }
    
    // 只有在怪物确凿死亡时，才将其历史最高生命值兑现为赏金！
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        // 如果死亡被阻止（比如复活），则不发赏金
        if (!creature.IsEnemy) return Task.CompletedTask;
        // 做最后一次更新，捕捉转阶段/复活瞬间可能提升的最大生命值
        UpsertEnemyMaxHpTracker(creature);
        // 怪物死亡，赏金兑现，通知 UI 更新右上角面板
        if (_enemyMaxHpTracker.TryGetValue(creature, out var record))
        {
            // 计算尚未发放赏金的血量差值
            var unrewardedHp = record.MaxHpSeen - record.MaxHpRewarded;
            if (unrewardedHp > 0)
            {
                Flash();
                CombatTotalEnemyMaxHp += unrewardedHp; // 累加至右上角赏金池
                record.MaxHpRewarded += unrewardedHp;  // 更新已兑现记录，防止反复刷钱
            }
        }
        
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
        _enemyMaxHpTracker.Clear(); // 内存清理
        return Task.CompletedTask;
    }
}