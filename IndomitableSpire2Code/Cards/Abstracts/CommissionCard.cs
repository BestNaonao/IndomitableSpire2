using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

[Pool(typeof(QuestCardPool))]
public abstract class CommissionCard(TargetType target) 
    : IndomitableSpire2Card(0, CardType.Quest, CardRarity.Quest, target)
{
    private Player? _delegator; // 必须声明后备字段
    
    // 记录是谁派发了这张委托
    public Player? Delegator
    {
        get => _delegator;
        set
        {
            _delegator = value;
            ((StringVar) DynamicVars["DelegatorName"]).StringValue = value == null ? "" :
                PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, value.NetId);
        }
    }
    
    // 仅限多人模式
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    
    // 子类必须实现的最大进度初始值
    protected abstract int InitialMaxProgressAmount { get; }
    
    // 注册进度变量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new("Progress", 0m),
        new("MaxProgress", InitialMaxProgressAmount),
        new StringVar("DelegatorName")
    ];
    
    private int CurrentProgress => DynamicVars["Progress"].IntValue;
    private int MaxProgressAmount => DynamicVars["MaxProgress"].IntValue;
    private bool IsCompleted => CurrentProgress >= MaxProgressAmount;
    
    // 当进度满时，卡牌自动高亮闪烁金光
    protected override bool ShouldGlowGoldInternal => IsCompleted;
    
    // 核心限制：进度未满时绝对不可打出
    protected override bool IsPlayable => base.IsPlayable && IsCompleted;
    
    /// <summary>
    /// 供子类在各大钩子（Hook）中调用的增加进度方法
    /// </summary>
    protected void AddProgress(int amount)
    {
        if (amount > 0 && !IsCompleted)
            DynamicVars["Progress"].BaseValue = Math.Min(MaxProgressAmount, CurrentProgress + amount);
    }
    
    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 避免被倾泻、抉择抉择等牌无条件打出获得奖励
        if (!IsCompleted) return;
        
        // 1. 给予持卡方（自己）奖励
        await GrantReward(choiceContext, Owner);
        
        // 2. 给予委托方（队友）奖励，需判定委托方存在、不是自己（单人模式下防重），且仍然存活
        if (Delegator != null && Delegator != Owner && Delegator.Creature.IsAlive)
            await GrantReward(choiceContext, Delegator);
        
        // 3. 自定义居中飞行特效，逃生舱无声移除
        await PlayCommissionFlyVfx();
        await CardPileCmd.RemoveFromCombat(this, skipVisuals: true);
    }
    
    // 强制返回 PileType.None 作为保险
    protected override PileType GetResultPileTypeForCardPlay() => IsCompleted ? PileType.None : base.GetResultPileTypeForCardPlay();
    
    /// <summary>
    /// 子类必须实现的奖励发放逻辑
    /// </summary>
    protected abstract Task GrantReward(PlayerChoiceContext choiceContext, Player player);
    
    /// <summary>
    /// 自定义的委托完成吸收特效。提取自原版 PlayPowerCardFlyVfx。
    /// </summary>
    private async Task PlayCommissionFlyVfx()
    {
        // 如果是在无头模式或快速测试中，跳过表现
        if (NCombatRoom.Instance == null) return;
        
        // 尝试获取当前桌面上这张卡的节点
        if (NCard.FindOnTable(this) is { } node)
        {
            // 核心修改 1：强制将节点移动到打出区的标准位置（屏幕中央偏下）
            // 覆盖原版“停留在鼠标松开位置”的行为
            node.GlobalPosition = PileType.Play.GetTargetPosition(node);
            
            // 触发一张卡牌凭空缩放弹出的动画
            node.CreateTween().Parallel()
                .TweenProperty(node, "scale", Vector2.One, 0.1)
                .From(Vector2.Zero)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            
            await Cmd.CustomScaledWait(0.1f, 0.8f);
            
            // 实例化原生能力牌的吸收光芒特效并附加到这张卡上
            var flyVfx = NCardFlyPowerVfx.Create(node);
            NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(flyVfx);
            
            // 播放动画并等待结束
            await TaskHelper.RunSafely(flyVfx!.PlayAnim());
            var duration = flyVfx.GetDuration();
            await Cmd.CustomScaledWait(duration * 0.2f, duration);
            
            // 动画播放完毕后，隐藏卡牌本体，为随后的“无声移除”做准备
            node.Visible = false;
        }
    }
}