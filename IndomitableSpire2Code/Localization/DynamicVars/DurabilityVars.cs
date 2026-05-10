using BaseLib.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;

/// <summary>
/// 用于舰载机卡的耐久度变量，用于记录当前耐久和展示耐久与预测损失。
/// </summary>
public class DurabilityVar : DynamicVar
{
    // 记录预览时将要损失的耐久度
    private int _predictedLoss;
    
    public const string DefaultName = "Durability";
    
    public DurabilityVar(decimal baseValue) : base(DefaultName, baseValue) { this.WithTooltip(); }
    
    public DurabilityVar(string name, decimal baseValue) : base(name, baseValue) { this.WithTooltip(); }
    
    /// <summary>
    /// 核心引擎钩子：每当鼠标拖拽卡牌指向不同目标，或者按数字键群攻预览时，引擎会频繁调用此方法
    /// </summary>
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        // 先调用基类，确保数值底座正确
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
        _predictedLoss = 0; // 重置预测
        
        // 必须是一张舰载机牌且在战斗中
        if (card is not CarrierAircraftCard aircraftCard || aircraftCard.CombatState == null) return;
        
        // 模仿 Hook.ModifyDamage，构造本次预览可能波及的敌方目标列表
        var potentialTargets = new List<Creature>();
        
        // 1. 如果是群攻预览 (AOE)，或者多目标选择，抓取全场敌人
        if (previewMode == CardPreviewMode.MultiCreatureTargeting && 
            aircraftCard.TargetType is TargetType.AllEnemies or TargetType.RandomEnemy)
            potentialTargets.AddRange(aircraftCard.CombatState.HittableEnemies);
        
        // 2. 如果是单体指向，并且当前鼠标确切指着一个存活的敌人
        else if (target is { IsAlive: true, IsEnemy: true })
            potentialTargets.Add(target);
        
        // 3. 调用舰载机的纯计算函数，获取预测损失
        if (potentialTargets.Count != 0)
            _predictedLoss = aircraftCard.CalculateDurabilityLoss(potentialTargets);
        
        // 我们利用 PreviewValue 来告诉基类的 ToHighlightedString 是否发生了“变化”（虽然我们自己会接管文本格式）
        if (_predictedLoss > 0)
             // 故意让预览值变小，触发 STS2 底层的“降低变红”逻辑（用于给其他可能探测这个变量的 Mod 一个交代）
             PreviewValue = BaseValue - _predictedLoss; 
    }
    
    /// <summary>
    /// 接管向游戏文本系统输出格式化字符串的最高权限，返回值将直接替换卡牌描述中的 {Durability}
    /// 当存在预测损失，并且当前的基础耐久度足够扣除时（没归零）拼接出的格式：(A-[red]B[/red])
    /// 正常情况（不在预览状态，或者预测不掉血），只显示当前数字
    /// </summary>
    public override string ToString() => 
        _predictedLoss > 0 && BaseValue > 0 ? $"({IntValue}-[red]{_predictedLoss}[/red])" : IntValue.ToString();
}

public class MaxDurabilityVar : DynamicVar
{
    public const string DefaultName = "MaxDurability";
    
    public MaxDurabilityVar(decimal baseValue) : base(DefaultName, baseValue) { }
    
    public MaxDurabilityVar(string name, decimal baseValue) : base(name, baseValue) { }
}