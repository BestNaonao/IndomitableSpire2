using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class VictoriousSongPower : IndomitablePower, IAfterCreatureEscapedSubscriber
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new StringVar("TargetName"), new BoolVar("HasTarget")];
    
    // 每次结算都重新查找，保留所有当前生命值并列最高的存活敌人。
    private IReadOnlyList<Creature> HighestHpEnemies()
    {
        if (!IsMutable || CombatState.Enemies.Where(enemy => enemy.IsAlive).ToList() is not
                { Count : > 0 } enemies) return [];
        var highestHp = enemies.Max(enemy => enemy.CurrentHp);
        return enemies.Where(enemy => enemy.CurrentHp == highestHp).ToList();
    }
    
    private void SyncTarget()
    {
        var enemies = HighestHpEnemies();
        ((StringVar)DynamicVars["TargetName"]).StringValue = string.Join(", ", enemies.Select(enemy => enemy.Name));
        ((BoolVar)DynamicVars["HasTarget"]).BoolVal = enemies.Count > 0;
    }
    
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncTarget();
        return Task.CompletedTask;
    }
    
    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this) return Task.CompletedTask;
        SyncTarget();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
    
    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature.IsEnemy) SyncTarget();
        return Task.CompletedTask;
    }
    
    public override Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (creature.IsEnemy) SyncTarget();
        return Task.CompletedTask;
    }
    
    public override Task AfterDeath(
        PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature.IsEnemy) SyncTarget();
        return Task.CompletedTask;
    }
    
    // 使用自定义钩子接口重写，重新同步最高血量目标
    public Task AfterCreatureEscaped(Creature creature)
    {
        if (creature.IsEnemy) SyncTarget();
        return Task.CompletedTask;
    }
    
    public override decimal ModifyDamageAdditive(
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!props.IsPoweredAttack() || HighestHpEnemies() is not { Count : > 0 } enemies) return 0M;
        if (dealer == Owner && target != null && enemies.Contains(target)) return Amount;
        if (target == Owner && dealer != null && enemies.Contains(dealer)) return -Amount;
        return 0M;
    }
}