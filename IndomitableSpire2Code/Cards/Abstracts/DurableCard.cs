using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
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