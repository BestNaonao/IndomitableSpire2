using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class Cleanup() : IndomitableCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // 提供“消耗”关键字的悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    
    // 注册变量：基础 7 点伤害，抽 1 张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new DamageVar(7M, ValueProp.Move),
        new CardsVar(1)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        // 1. 先造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash") // 挥扫的视觉特效
            .Execute(choiceContext);
        
        // 2. 选择一张手牌消耗
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
        var selectedCard = (await CardSelectCmd.FromHand(
            choiceContext, Owner, prefs, null, this
        )).FirstOrDefault();
        
        if (selectedCard != null)
        {
            // 【核心细节】：在牌被消耗前，先记录它的类型
            var isGarbage = selectedCard.Type is CardType.Status or CardType.Curse;
            
            // 消耗选中的牌
            await CardCmd.Exhaust(choiceContext, selectedCard);
            
            // 3. 如果是状态牌或诅咒牌，触发奖励抽牌
            if (isGarbage)
            {
                // 等待短暂的视觉延迟，让消耗动画先播放完毕，体验更平滑
                await Cmd.Wait(0.1f);
                await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
            }
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：伤害 +3（变为10点）
        DynamicVars.Damage.UpgradeValueBy(3M);
        DynamicVars.Cards.UpgradeValueBy(1M);
    }
}