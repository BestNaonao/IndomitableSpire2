using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class Retire() : IndomitableCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 提供“消耗”与“舰载机”关键字的悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft)
    ];
    
    // 注册获得干劲的变量：初始 10 点
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MotivationGainVar(10M)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 1. 从抽牌堆选择一张牌（参考《洁净》）
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
        var selectedCard = (await CardSelectCmd.FromCombatPile(
            choiceContext, 
            PileType.Draw.GetPile(Owner), 
            Owner, 
            prefs
        )).FirstOrDefault();
        
        if (selectedCard != null)
        {
            // 【核心细节】：在被消耗送入消耗堆前，判断它是否为舰载机牌
            var isAircraft = selectedCard.IsCarrierAircraft();
            // 消耗选中的牌
            await CardCmd.Exhaust(choiceContext, selectedCard);
            // 稍作等待，让消耗特效播放完，视觉体验更佳
            await Cmd.Wait(0.1f);
            
            // 2. 根据牌的类型执行不同分支
            if (isAircraft)
            {
                // 分支 A：退役了舰载机，提供战术升级（参考《武装》）
                if (IsUpgraded)
                {
                    // 升级手牌中的所有可升级牌
                    foreach (var card in PileType.Hand.GetPile(Owner).Cards.Where(c => c.IsUpgradable))
                        CardCmd.Upgrade(card);
                }
                else
                {
                    // 调出选择界面，升级手牌中的一张牌
                    var cardToUpgrade = await CardSelectCmd.FromHandForUpgrade(choiceContext, Owner, this);
                    if (cardToUpgrade != null) CardCmd.Upgrade(cardToUpgrade);
                }
            }
            else
            {
                // 分支 B：退役了非舰载机牌，获得干劲
                await PowerCmd.Apply<MotivationPower>(
                    choiceContext: choiceContext, 
                    target: Owner.Creature, 
                    amount: DynamicVars.MotivationGain().BaseValue, 
                    applier: Owner.Creature, 
                    cardSource: this
                );
            }
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：未升级时获得 10 干劲，升级后获得 20 干劲
        DynamicVars.MotivationGain().UpgradeValueBy(10M);
    }
}