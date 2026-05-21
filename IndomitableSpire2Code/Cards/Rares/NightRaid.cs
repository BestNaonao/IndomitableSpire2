using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class NightRaid() : IndomitableCard(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    // 用于记录被选中的攻击牌，在全局伤害钩子中进行特判识别
    private CardModel? _selectedCard;
    
    // 注册变量：需要 30 点干劲
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new MotivationRequireVar(30M),
        new DamageMultiplierVar(2M)
    ];
    
    // 核心限制：干劲不足时不可打出
    protected override bool IsPlayable => this.MeetsMotivationRequirement();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 配置选牌界面
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1)
        {
            PretendCardsCanBePlayed = true // 让手牌在选择界面中亮起，提供更好的视觉反馈
        };
        
        // 从手牌中选择一张攻击牌，并记录这张牌，以便在随后的伤害计算中为其提供特判加成
        _selectedCard = (await CardSelectCmd.FromHand(
            choiceContext, 
            Owner, 
            prefs, 
            c => c.Type == CardType.Attack, 
            this
        )).FirstOrDefault();
        
        if (_selectedCard != null)
        {
            try
            {
                // 【核心引导机制】：将夜袭技能牌指定的目标 (cardPlay.Target) 传递给自动打出的攻击牌！
                // 如果是单体攻击，它将精准命中 cardPlay.Target；如果是群攻/随机攻击，底层会自动忽略。
                await CardCmd.AutoPlay(choiceContext, _selectedCard, cardPlay.Target);
            }
            finally
            {
                // 无论打出过程是否发生异常，都必须清空记录，防止后续其他卡牌意外吃到加成或造成内存泄漏
                _selectedCard = null;
            }
        }
    }
    
    // 【核心特判机制】：利用卡牌在战斗堆中会监听全局钩子的特性，拦截并修改伤害
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // 1. 确保造成伤害的卡正是我们刚才记录的那张选定的牌，且这是一次卡牌攻击伤害
        // 2. 确保目标存在、是怪物、并且通过我们的扩展方法判定其拥有“睡眠”意图
        return cardSource != null && cardSource == _selectedCard && props.IsPoweredAttack() &&
               target?.Monster != null && target.Monster.IntendsToSleep()
            ? DynamicVars.DamageMultiplier().BaseValue : 1M;
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：双倍变三倍
        DynamicVars.DamageMultiplier().UpgradeValueBy(1M);
    }
}