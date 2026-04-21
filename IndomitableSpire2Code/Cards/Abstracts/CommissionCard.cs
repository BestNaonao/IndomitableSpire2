using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

[Pool(typeof(TokenCardPool))]
public abstract class CommissionCard(CardType type, CardRarity rarity,TargetType target) 
    : CustomCardModel(0, type, rarity, target)
{
    // 记录是谁派发了这张委托
    public Player? Delegator { get; set; }
    
    // 仅限多人模式
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    
    // 子类必须实现的最大进度
    protected abstract int MaxProgressAmount { get; }
    
    // 强制赋予委托和消耗关键字
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.Commission, CardKeyword.Exhaust];
    
    // 注册进度变量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new("Progress", 0m),
        new("MaxProgress", MaxProgressAmount)
    ];
    
    public int CurrentProgress => (int)DynamicVars["Progress"].BaseValue;
    
    // 当进度满时，卡牌自动高亮闪烁金光
    protected override bool ShouldGlowGoldInternal => CurrentProgress >= MaxProgressAmount;
    
    // 核心限制：进度未满时绝对不可打出
    protected override bool IsPlayable => base.IsPlayable && CurrentProgress >= MaxProgressAmount;
    
    /// <summary>
    /// 供子类在各大钩子（Hook）中调用的增加进度方法
    /// </summary>
    protected void AddProgress(int amount)
    {
        if (amount <= 0 || CurrentProgress >= MaxProgressAmount) return;
        var newProgress = Math.Min(MaxProgressAmount, CurrentProgress + amount);
        DynamicVars["Progress"].BaseValue = newProgress;
    }
    
    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 给予持卡方（自己）奖励
        await GrantReward(choiceContext, Owner);
        
        // 2. 给予委托方（队友）奖励
        // 判定委托方存在、不是自己（单人模式下防重），且仍然存活
        if (Delegator != null && Delegator != Owner && Delegator.Creature.IsAlive)
            await GrantReward(choiceContext, Delegator);
    }
    
    /// <summary>
    /// 子类必须实现的奖励发放逻辑
    /// </summary>
    protected abstract Task GrantReward(PlayerChoiceContext choiceContext, Player player);
}