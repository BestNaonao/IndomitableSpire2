using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

public abstract class DurableCard(
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
    
    // 升级时提升的耐久值
    protected abstract int UpgradeDurabilityAmount { get; set; }
    
    // 注册耐久度动态变量
    protected sealed override IEnumerable<DynamicVar> CanonicalVars => 
        [new DurabilityVar(MaxDurability), new MaxDurabilityVar(MaxDurability), ..AdditionalVars];
    
    protected abstract IEnumerable<DynamicVar> AdditionalVars { get; }
    
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
            var originalLoss = await CalculateDurabilityLossAfterPlay(choiceContext, cardPlay);
            var loss = CustomHook.ModifyDurabilityLossInCombat(
                CombatState, this, originalLoss, out var models);
            // 设置耐久变量和消耗应该保证原子化执行
            if (loss > 0)
            {
                DynamicVars.Durability().BaseValue = Math.Max(0, DynamicVars.Durability().BaseValue - loss);
                if (this.OutOfDurability()) await CardCmd.Exhaust(choiceContext, this);
            }
            // 处理后置钩子
            await CustomHook.AfterModifyingDurabilityLossInCombat(CombatState, this, models);
        }
        finally
        {
            CleanupAfterDurabilityLoss();
        }
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