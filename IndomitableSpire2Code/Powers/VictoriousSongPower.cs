using BaseLib.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class VictoriousSongPower : IndomitablePower, IHasSecondAmount
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new("DamageReduction", 0M), 
        new StringVar("TargetName"), 
        new BoolVar("HasTarget")
    ];
    
    public string GetSecondAmount() => DynamicVars["DamageReduction"].IntValue.ToString();
    
    // 每次结算都重新查找；生命值并列时，按战场敌人顺序选择第一个，避免随机切换。
    private Creature? HighestHpEnemy => IsMutable 
        ? CombatState.Enemies.Where(enemy => enemy.IsAlive).MaxBy(enemy => enemy.CurrentHp) : null;
    
    protected override string SmartDescriptionLocKey
    {
        get
        {
            // 悬浮提示由引擎随战斗状态刷新。这里也同步目标，覆盖逃跑、直接改血等情况。
            if (IsMutable) SyncTarget();
            return base.SmartDescriptionLocKey;
        }
    }
    
    private void SyncTarget()
    {
        var enemy = HighestHpEnemy;
        ((StringVar)DynamicVars["TargetName"]).StringValue = enemy?.Name ?? string.Empty;
        ((BoolVar)DynamicVars["HasTarget"]).BoolVal = enemy != null;
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
        // amount 是经过能力数量修正后的本次实际增量，而不是累计 Amount。每次打出本牌分别累计减伤：3 -> 1、4 -> 2，翻倍后为 6 -> 4、8 -> 6。
        if (amount > 0 && cardSource is VictoriousSong)
            DynamicVars["DamageReduction"].BaseValue += Math.Max(0, (int)amount - 2);
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
    
    public override decimal ModifyDamageAdditive(
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!props.IsPoweredAttack() || HighestHpEnemy is not {} enemy) return 0M;
        if (dealer == Owner && target == enemy) return Amount;
        if (target == Owner && dealer == enemy) return -DynamicVars["DamageReduction"].BaseValue;
        return 0M;
    }
}
