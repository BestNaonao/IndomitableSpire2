using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

/// <summary>
/// 升降级改变基础耐久；损失、维修和最大耐久增减另外记录，降级重建变量后再恢复。
/// 外部不要直接写入本类的 Durability / MaxDurability 动态变量，否则会绕过历史记录。
/// 游戏效果应调用 DurabilityCmd；纯数值操作使用下列四个 Internal 方法。
/// Internal 方法不播放动画、不触发钩子或消耗卡牌，也不得用于处理升级。
/// </summary>
public abstract class DurableCard(
    int baseCost, 
    CardType type, 
    CardRarity rarity, 
    TargetType target, 
    bool showInCardLibrary = true, 
    bool autoAdd = true
    ) : IndomitableCard(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
{
    public const int DurabilityLimit = 999999999;
    // 基础最大耐久
    protected abstract int MaxDurability { get; set; }
    // 升级时提升的耐久值
    protected abstract int UpgradeDurabilityAmount { get; set; }
    // 注册耐久度动态变量
    protected sealed override IEnumerable<DynamicVar> CanonicalVars => 
        [new DurabilityVar(MaxDurability), new MaxDurabilityVar(MaxDurability), ..AdditionalVars];
    
    protected abstract IEnumerable<DynamicVar> AdditionalVars { get; }
    
    // 记录实际耐久相对于当前升级基础值的有效偏移；降级发生裁剪时，按裁剪后的实际值重新计算偏移。
    private decimal _extraDurability;
    private decimal _extraMaxDurability;
    private decimal UpgradedBaseDurability => MaxDurability + (decimal)CurrentUpgradeLevel * UpgradeDurabilityAmount;
    
    /// <returns>实际损失的当前耐久。</returns>
    public int LoseDurabilityInternal(int amount)
    {
        AssertMutable();
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var durability = DynamicVars.Durability();
        var loss = Math.Min(amount, durability.IntValue);
        if (loss <= 0) return 0;
        durability.BaseValue -= loss;
        _extraDurability = durability.BaseValue - UpgradedBaseDurability;
        return loss;
    }
    
    /// <returns>实际恢复的当前耐久，不超过最大耐久。</returns>
    public int GainDurabilityInternal(int amount)
    {
        AssertMutable();
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var durability = DynamicVars.Durability();
        var gain = Math.Min(amount, DynamicVars.MaxDurability().IntValue - durability.IntValue);
        if (gain <= 0) return 0;
        durability.BaseValue += gain;
        _extraDurability = durability.BaseValue - UpgradedBaseDurability;
        return gain;
    }
    
    /// <summary>降低最大耐久，并记录由上限降低导致的当前耐久损失。</summary>
    /// <returns>实际损失的最大耐久。</returns>
    public int LoseMaxDurabilityInternal(int amount)
    {
        AssertMutable();
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var maximum = DynamicVars.MaxDurability();
        var loss = Math.Min(amount, maximum.IntValue);
        if (loss <= 0) return 0;
        maximum.BaseValue -= loss;
        _extraMaxDurability = maximum.BaseValue - UpgradedBaseDurability;
        LoseDurabilityInternal(Math.Max(0, DynamicVars.Durability().IntValue - maximum.IntValue));
        return loss;
    }
    
    /// <summary>仅增加最大耐久；DurabilityCmd.GainMaxDurability 另外恢复等量当前耐久。</summary>
    /// <returns>实际增加的最大耐久。</returns>
    public int GainMaxDurabilityInternal(int amount)
    {
        AssertMutable();
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        var maximum = DynamicVars.MaxDurability();
        var gain = Math.Min(amount, DurabilityLimit - maximum.IntValue);
        if (gain <= 0) return 0;
        maximum.BaseValue += gain;
        _extraMaxDurability = maximum.BaseValue - UpgradedBaseDurability;
        return gain;
    }
    
    /// 返回本次打出基础耐久损失（未接收修改）。
    protected virtual Task<int> CalculateDurabilityLossAfterPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) 
        => Task.FromResult(0);
    
    /// 计算后的清理工作。
    protected virtual void CleanupAfterDurabilityLoss() {}
    
    /// 重写 AfterCardPlayed 方法以计算和处理耐久损失
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != this || CombatState == null) return;
        try
        {
            var loss = await CalculateDurabilityLossAfterPlay(choiceContext, cardPlay);
            await DurabilityCmd.LoseDurability(choiceContext, this, loss);
        }
        finally
        {
            CleanupAfterDurabilityLoss();
        }
    }
    
    /// <summary>升级专用：标记升级预览，同时提高当前耐久和最大耐久；不调用 Internal 方法。</summary>
    protected void UpgradeDurability()
    {
        if (UpgradeDurabilityAmount <= 0) return;
        DynamicVars.MaxDurability().UpgradeValueBy(UpgradeDurabilityAmount);
        DynamicVars.Durability().UpgradeValueBy(UpgradeDurabilityAmount);
    }
    
    protected override void AfterDowngraded()
    {
        // 先按照记录回复耐久的变化，再记录新的变化（因为裁剪可能会导致变化记录有误）
        DynamicVars.MaxDurability().BaseValue = 
            Math.Clamp(UpgradedBaseDurability + _extraMaxDurability, 0M, DurabilityLimit);
        DynamicVars.Durability().BaseValue = 
            Math.Clamp(UpgradedBaseDurability + _extraDurability, 0M, DynamicVars.MaxDurability().BaseValue);
        _extraDurability = DynamicVars.Durability().BaseValue - UpgradedBaseDurability;
        _extraMaxDurability = DynamicVars.MaxDurability().BaseValue - UpgradedBaseDurability;
    }
}