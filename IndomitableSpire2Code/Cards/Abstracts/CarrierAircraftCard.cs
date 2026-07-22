using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

public abstract class CarrierAircraftCard(
    int baseCost, 
    CardType type, 
    CardRarity rarity, 
    TargetType target, 
    bool showInCardLibrary = true, 
    bool autoAdd = true
    ) : DurableCard(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
{
    // 强制赋予基础舰载机的 Tag 和 ExtraHoverTips
    protected abstract IEnumerable<CardTag> SubclassTags { get; }
    
    protected override HashSet<CardTag> CanonicalTags => [IndomitableTags.CarrierAircraft, ..SubclassTags];
    
    private IReadOnlyList<IReadOnlyList<DamageResult>> _lastDamageResults = [];
    
    /// <summary>
    /// 舰载机专用的打出抽象方法。子类在此处编写伤害或辅助逻辑。
    /// </summary>
    /// <returns>返回多段伤害结果列表的集合，若无伤害（如纯技能牌）可返回空列表</returns>
    protected abstract Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay);
    
    // 封装原本的 OnPlay，使其成为模板方法（Template Method）
    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 调用子类必须实现的抽象方法，接收新版本的多段攻击结果
        _lastDamageResults = (await OnAircraftPlay(choiceContext,cardPlay))
            .Select(x=>x.ToList())
            .ToList();
    }
    
    // 2. 重写父类计算打出后的耐久损失
    protected override Task<int> CalculateDurabilityLossAfterPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // SelectMany 将所有攻击段数的结果合并为一个平级的流，Distinct 保证多段攻击打在同一个敌人身上时，不重复触发多次防空判定
        var hitEnemies = _lastDamageResults
            .SelectMany(hitList => hitList) 
            .Select(r => r.Receiver)
            .Where(c => c is { IsAlive: true, IsEnemy: true })
            .Distinct();
        return Task.FromResult(CalculateDurabilityLoss(hitEnemies));
    }
    
    // 3. 重写父类方法，在打出卡牌后将上一次伤害结果的记录清空
    protected override void CleanupAfterDurabilityLoss() => _lastDamageResults = [];
    
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
}