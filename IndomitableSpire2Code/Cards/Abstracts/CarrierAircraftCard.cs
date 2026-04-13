using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
    protected abstract int MaxDurability { get; set; }

    // 强制赋予基础舰载机的 Tag 和 Keyword
    protected abstract IEnumerable<CardKeyword> SubclassKeywords { get; }
    protected abstract IEnumerable<CardTag> SubclassTags { get; }

    public override IEnumerable<CardKeyword> CanonicalKeywords => 
        [IndomitableKeywords.CarrierAircraft, ..SubclassKeywords];

    protected override HashSet<CardTag> CanonicalTags => 
        [IndomitableTags.CarrierAircraft, ..SubclassTags];

    // 注册耐久度动态变量，用于 UI 展现
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new("Durability", MaxDurability),
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
            CalculateAndApplyDurabilityLoss(hitEnemies);
        }

        // 3. 如果耐久归零，触发消耗
        if (DynamicVars["Durability"].BaseValue <= 0)
        {
            await CardCmd.Exhaust(choiceContext, this);
            // 可选：播放飞机坠毁的音效，强化反馈感
            // SfxCmd.Play("event:/sfx/enemy/enemy_attacks/automaton/automaton_death");
        }
    }

    /// <summary>
    /// 舰载机专用的打出抽象方法。
    /// 子类在此处编写伤害或辅助逻辑，并按需调用 CalculateAndApplyDurabilityLoss。
    /// </summary>
    /// <returns>返回伤害结果列表，若无伤害（如纯技能牌）可返回 null</returns>
    protected abstract Task<IEnumerable<DamageResult>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    /// <summary>
    /// 完全向子类隐藏的耐久损失方法。根据受击目标集合的意图计算耐久损失。
    /// </summary>
    private void CalculateAndApplyDurabilityLoss(IEnumerable<Creature> targets)
    {
        if (CombatState == null) return;

        var maxDurabilityLoss = (from enemy in targets.Select(c => c.Monster).OfType<MonsterModel>()
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

        // 扣除耐久
        if (maxDurabilityLoss > 0)
            DynamicVars["Durability"].BaseValue = Math.Max(0, DynamicVars["Durability"].BaseValue - maxDurabilityLoss);
        // 触发变量更新以刷新卡面 UI
    }
}