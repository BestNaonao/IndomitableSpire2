using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class WorkArrangement() : IndomitableCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 注册变量：抽 2 张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new CardsVar(2),
        new MotivationConsumeVar(10M)   // 使用专属变量类
    ];
    
    // 核心限制：必须有足够的干劲才能打出
    protected override bool IsPlayable => this.CanAffordMotivationCost();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 消耗干劲：通过施加负数的能力层数来实现扣除
        await PowerCmd.Apply<MotivationPower>(
            target: Owner.Creature, 
            amount: -DynamicVars.MotivationConsume().BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );
        
        // 2. 抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        
        // 3. 呼出选牌界面，从手牌中选择 1 张牌，SelectionScreenPrompt 会自动读取本地化 JSON 中的 selectionScreenPrompt 字段
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var card = (await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, this)).FirstOrDefault();
        
        // 4. 如果成功选择了卡牌，将其移动到抽牌堆底部
        if (card != null)
            await CardPileCmd.Add(card, PileType.Draw);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：抽牌数 +1 (变为抽 3)
        DynamicVars.Cards.UpgradeValueBy(1M);
    }
}