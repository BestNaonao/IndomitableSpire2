using BaseLib.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

public abstract class CarrierAircraftCard(
    int baseCost, 
    CardType type, 
    CardRarity rarity, 
    TargetType target, 
    bool showInCardLibrary = true, 
    bool autoAdd = true
    ) : IndomitableCard(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
{
    // 基础最大耐久
    protected abstract int MaxDurability { get; set; }
    // 升级时提升的耐久值（子类可覆写，默认为 2）
    protected abstract int UpgradeDurabilityAmount { get; set; }
    
    // 强制赋予基础舰载机的 Tag 和 ExtraHoverTips
    protected abstract IEnumerable<CardTag> SubclassTags { get; }
    
    protected override HashSet<CardTag> CanonicalTags => 
        [IndomitableTags.CarrierAircraft, ..SubclassTags];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft)
    ];
    
    // 注册耐久度动态变量，用于 UI 展现：使用我们自定义的 DurabilityVar 替代普通的 DynamicVar
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new DurabilityVar("Durability", MaxDurability).WithTooltip(),
        new("MaxDurability", MaxDurability)
    ];
    
    // 封装原本的 OnPlay，使其成为模板方法（Template Method）
    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 调用子类必须实现的抽象方法
        var damageResults = await OnAircraftPlay(choiceContext, cardPlay);
        
        // 2. 动态判定：只有当子类返回了有效伤害结果时，才视为执行了轰炸并遭到防空火力反击
        if (damageResults != null)
        {
            // 极其精准：直接从伤害结果中提取真正受到波及且存活的敌方怪物
            var hitEnemies = damageResults
                .Select(r => r.Receiver)
                .Where(c => c is { IsAlive: true, IsEnemy: true });
            
            // 【核心改动】：先计算，再应用
            var loss = CalculateDurabilityLoss(hitEnemies);
            if (loss > 0)
                DynamicVars.Durability().BaseValue = Math.Max(0, DynamicVars.Durability().BaseValue - loss);
        }
        
        // 3. 【回归本质】：如果在战斗结算中耐久归零，立即手动将其送入消耗堆！利用引擎的逃生舱机制，避免了预计算带来的滞后性
        if (this.OutOfDurability())
            await CardCmd.Exhaust(choiceContext, this);
    }
    
    // 4. 原生坠毁后摇：处理飞机损毁的特殊反馈。引擎在将卡牌移动到消耗堆后会自动调用这个虚方法
    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        // 只有当是因为耐久归零（而不是因为虚无或其他效果被消耗）时，才播放坠毁特效
        if (card == this && card.OutOfDurability() && !causedByEthereal)
        {
            // 在这里播放飞机坠毁的音效，强化反馈感
            // SfxCmd.Play("event:/sfx/enemy/enemy_attacks/automaton/automaton_death");
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 舰载机专用的打出抽象方法。子类在此处编写伤害或辅助逻辑。
    /// </summary>
    /// <returns>返回伤害结果列表，若无伤害（如纯技能牌）可返回 null</returns>
    protected abstract Task<IEnumerable<DamageResult>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay);
    
    /// <summary>
    /// 根据受击目标集合的攻击意图计算耐久损失。
    /// </summary>
    public int CalculateDurabilityLoss(IEnumerable<Creature> targets)
    {
        if (CombatState == null) return 0;
        // 直接计算并返回耐久损失
        return (from enemy in targets.Select(c => c.Monster).OfType<MonsterModel>()
                where enemy.Creature.IsAlive
                let singleDamage = enemy.GetIntentSingleDamage()
                let hitCount = enemy.GetIntentHitCount()
                where singleDamage > 0 && hitCount > 0
                let lossPerHit = singleDamage switch
                {
                    < 5 => 1,
                    < 10 => 2,
                    < 20 => 3,
                    < 40 => 4,
                    _ => 4 + singleDamage / 40
                }
                select lossPerHit * hitCount).Prepend(0)
            .Max();
    }
    
    /// <summary>
    /// 供子类在 OnUpgrade 中调用的打包升级方法，同时提高当前耐久和最大耐久，并同步更新 UI
    /// </summary>
    protected void UpgradeDurability()
    {
        if (UpgradeDurabilityAmount <= 0) return;
        DynamicVars.MaxDurability().UpgradeValueBy(UpgradeDurabilityAmount);
        DynamicVars.Durability().UpgradeValueBy(UpgradeDurabilityAmount);
    }
}