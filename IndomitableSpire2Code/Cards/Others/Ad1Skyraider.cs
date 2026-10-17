using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

public sealed class Ad1Skyraider() : WhiteEagleAircraftCard(6, TargetType.AnyEnemy)
{
    protected override int MaxDurability { get; set; } = 21;
    protected override int UpgradeDurabilityAmount { get; set; } = 5;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.LevelBomber];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.LevelBomber];
    protected override IEnumerable<(CardTag Tag, CardKeyword Keyword)> SynergyTypes =>
        [(IndomitableTags.StrikeFighter, IndomitableKeywords.StrikeFighter),
         (IndomitableTags.LevelBomber, IndomitableKeywords.LevelBomber),
         (IndomitableTags.DiveBomber, IndomitableKeywords.DiveBomber)];
    
    protected override IEnumerable<DynamicVar> AdditionalVars => 
    [
        new DamageVar(2M, ValueProp.Move), 
        new RepeatVar(6), 
        new DamageVar("AoeDamage", 10M, ValueProp.Move)
    ];
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        ArgumentNullException.ThrowIfNull(CombatState);
        var results = new List<IEnumerable<DamageResult>>();
        // 点杀与群伤共用一次攻击，活力在两部分伤害结束后才消耗。
        await using (var attackContext = await AttackCommand.CreateContextAsync(CombatState, choiceContext, cardPlay))
        {
            for (var hit = 0; hit < DynamicVars.Repeat.IntValue && cardPlay.Target.IsAlive &&
                              !CombatManager.Instance.IsOverOrEnding; hit++)
            {
                await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);
                var focused = (await CreatureCmd.Damage(
                    choiceContext, cardPlay.Target, DynamicVars.Damage, Owner.Creature, this, cardPlay))
                    .ToList();
                attackContext.AddHit(focused);
                results.Add(focused);
            }
            // 然后造成群体伤害，共用一个 attackContext 以共享活力加成。
            if (!CombatManager.Instance.IsOverOrEnding)
            {
                await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);
                var area = (await CreatureCmd.Damage(
                    choiceContext, CombatState.HittableEnemies, (DamageVar)DynamicVars["AoeDamage"],
                    Owner.Creature, this, cardPlay)).ToList();
                attackContext.AddHit(area);
                results.Add(area);
            }
        }
        return results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(2M);
        DynamicVars["AoeDamage"].UpgradeValueBy(4M);
        UpgradeDurability();
    }
}