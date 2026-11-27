using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Nodes;

internal partial class ArmorBreakPreviewController : Node
{
    // 缓存每个生物（主要是敌人）的破甲标签
    private readonly Dictionary<NCreature, ArmorBreakPreviewLabel> _labels = [];
    private NCreature? _single; // 是否处于单目标预览状态
    private bool _active; // 控制器是否激活
    private NTargetManager? _manager; // 引用全局目标管理器
    
    public override void _Ready()
    {
        CombatManager.Instance.StateTracker.CombatStateChanged += OnCombatChanged; // 监听战斗状态变化
        _manager = NTargetManager.Instance;
        _manager.TargetingEnded += OnTargetingEnded; // 监听目标选择结束
    }
    
    /// <summary>外部入口：显示目标破甲预览。</summary>
    /// <param name="single">单目标预览状态选择的生物节点，null 表示选择全体</param>
    internal void ShowTargets(NCreature? single)
    {
        _single = single;
        _active = true;
        Refresh();
    }
    
    private void OnCombatChanged(CombatState state)
    {
        if (_active && GetParent<NCardPlay>().Holder.CardModel?.CombatState == state) Refresh(); // 预览中的卡牌属于战斗才刷新
    }
    
    /// 核心刷新逻辑：决定显示哪些敌人的预览
    private void Refresh()
    {
        HideLabels(); // 先全部隐藏
        var card = GetParent<NCardPlay>().Holder.CardModel;
        if (card?.CombatState is not { } combat) return; // 卡牌战斗状态校验
        if (_single != null) // 单目标选择模式
        {
            if (IsInstanceValid(_single) && _single.Entity.IsHittable) ShowLoss(card, _single);
            return;
        }
        // 与原版全体和随机攻击绘制选框时使用相同的候选集，不抽取随机目标。
        if (NCombatRoom.Instance is not { } room) return; // 战斗房间校验
        foreach (var enemy in combat.HittableEnemies)
            if (room.GetCreatureNode(enemy) is { } node) ShowLoss(card, node);
    }
    
    /// 计算并显示单个敌人的格挡损失
    private void ShowLoss(CardModel card, NCreature node)
    {
        var loss = GetBlockLoss(card, node.Entity);
        if (loss <= 0) return;
        if (!_labels.TryGetValue(node, out var label) || !IsInstanceValid(label)) // 懒加载，展示时缺失才添加
        {
            label = ArmorBreakPreviewLabel.Create(node);
            if (label == null) return;
            _labels[node] = label;
        }
        label.ShowLoss(loss);
    }
    
    /// 读取攻击在扣格挡前的数值。卡面的 PreviewValue 还可能包含硬化外壳等生命损失修正。
    private static int GetBlockLoss(CardModel card, Creature target)
    {
        if (target.Block <= 0 || target.GetPower<ArmorBreakPower>() is not { Amount: > 0 } power) return 0;
        foreach (var variable in card.DynamicVars.Values)
        {
            ValueProp props;
            decimal amount;
            var dealer = card.Owner.Creature;
            switch (variable)
            {
                case DamageVar damage when damage.Props.IsPoweredAttack():
                    props = damage.Props;
                    amount = damage.BaseValue;
                    break;
                case CalculatedDamageVar calculated when calculated.Props.IsPoweredAttack():
                    props = calculated.Props;
                    amount = calculated.Calculate(target);
                    if (calculated.IsFromOsty) dealer = card.Owner.Osty;
                    break;
                case OstyDamageVar osty when osty.Props.IsPoweredAttack():
                    props = osty.Props;
                    amount = osty.BaseValue;
                    dealer = card.Owner.Osty;
                    break;
                default:
                    continue;
            }
            amount = Hook.ModifyDamage(card.Owner.RunState, card.CombatState, target, dealer, amount,
                props, card, null, ModifyDamageHookType.All, CardPreviewMode.Normal, out _);
            var loss = power.GetBlockLoss(new PendingHit(target, amount, props, dealer, card, null));
            if (loss > 0) return loss;
        }
        return 0;
    }
    
    // 当鼠标从特定目标移开时，如果该目标是当前单目标，则清除
    internal void ClearSingleTarget(NCreature node)
    {
        if (_single == node) Clear();
    }
    
    // 目标选择结束时，清除单目标预览
    private void OnTargetingEnded()
    {
        if (_single != null) Clear();
    }
    
    internal void Clear()
    {
        _active = false;
        _single = null;
        HideLabels();
    }
    
    private void HideLabels()
    {
        foreach (var label in _labels.Values.Where(IsInstanceValid)) label.Hide();
    }
    
    public override void _ExitTree()
    {
        CombatManager.Instance.StateTracker.CombatStateChanged -= OnCombatChanged;
        if (IsInstanceValid(_manager)) _manager!.TargetingEnded -= OnTargetingEnded;
        Clear();
        foreach (var label in _labels.Values.Where(IsInstanceValid)) label.QueueFree();
        _labels.Clear();
    }
}