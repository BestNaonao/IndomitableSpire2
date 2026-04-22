using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class DispatchCommission() : IndomitableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer)
{
    // 仅限多人模式可用
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 1. 【动态过滤】：获取衍生牌池中的所有牌，并筛选出所有继承自 CommissionCard 的子类
        IEnumerable<CardModel> allCommissions = ModelDb.CardPool<TokenCardPool>().AllCards.OfType<CommissionCard>();
        
        // 2. 【安全生成】：利用官方工厂随机抽取 1 张，它会自动绑定 CombatState 并处理各种联机状态！
        var card = CardFactory.GetDistinctForCombat(
            cardPlay.Target.Player!, 
            allCommissions, 
            1, 
            Owner.RunState.Rng.CombatCardGeneration
        ).FirstOrDefault();
        
        // 类型安全转换
        if (card is not CommissionCard generatedCard) return;
        
        // 3. 将打出母牌的玩家设为这单委托的“委托方（甲方）”
        generatedCard.Delegator = Owner;
        
        // 4. 继承升级状态
        if (IsUpgraded)
            CardCmd.Upgrade(generatedCard);
        
        // 5. 安全发送到队友手牌中
        await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Hand, true);
    }
    
    protected override void OnUpgrade()
    {
        // 升级将费用降为 0
        EnergyCost.UpgradeBy(-1);
    }
}