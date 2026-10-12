using BaseLib.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;

/// <summary>
/// 当前耐久变量；舰载机指向敌人时，通过 PreviewValue 展示防空造成的预计耐久损失。
/// 维修先修改实际值再展示，不使用额外的恢复预测状态。
/// </summary>
public class DurabilityVar : DynamicVar
{
    // 记录指向敌人时预计损失的耐久，供描述中的 {Durability} 格式化使用。
    private int _predictedLoss;
    public int PredictedHeal { get; set; }
    
    public const string DefaultName = "Durability";
    
    public DurabilityVar(decimal baseValue) : base(DefaultName, baseValue) { this.WithTooltip(); }
    
    public DurabilityVar(string name, decimal baseValue) : base(name, baseValue) { this.WithTooltip(); }
    
    /// <summary>
    /// 核心引擎钩子：每当鼠标拖拽卡牌指向不同目标，或者按数字键群攻预览时，引擎会频繁调用此方法
    /// </summary>
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        // 卡面刷新会先 ClearPreview，再调用此钩子；这里只重算当前目标的预测损失。
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
            _predictedLoss = CustomHook.ModifyDurabilityLossInCombat(
                aircraftCard.CombatState, aircraftCard, aircraftCard.CalculateDurabilityLoss(potentialTargets), out _);
        
        // 数字表盘读取 PreviewValue：显示指向敌人时的预计剩余耐久，不改变实际值。
        if (_predictedLoss > 0)
            PreviewValue = BaseValue - _predictedLoss;
    }
    
    /// <summary>
    /// 接管向游戏文本系统输出格式化字符串的最高权限，返回值将直接替换卡牌描述中的 {Durability}
    /// 当存在预测损失，并且当前的基础耐久度足够扣除时（没归零）拼接出的格式：(A-[red]B[/red])
    /// 正常情况（不在预览状态，或者预测不掉血），只显示当前数字
    /// </summary>
    public override string ToString() => 
        PredictedHeal > 0 
            ? $"({IntValue}+[green]{PredictedHeal}[/green])" 
            : _predictedLoss > 0 && BaseValue > 0 
                ? $"({IntValue}-[red]{_predictedLoss}[/red])" 
                : IntValue.ToString();
}

public class MaxDurabilityVar : DynamicVar
{
    public const string DefaultName = "MaxDurability";
    
    public MaxDurabilityVar(decimal baseValue) : base(DefaultName, baseValue) { }
    
    public MaxDurabilityVar(string name, decimal baseValue) : base(name, baseValue) { }
}