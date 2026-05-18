using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class EmergencyTakeoff() : IndomitableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放技能动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 获取抽牌堆的所有卡牌
        var drawPile = PileType.Draw.GetPile(Owner).Cards;
        
        // 1. 优先尝试寻找舰载机牌，如果没有舰载机牌，则退而求其次寻找攻击牌
        var validCards = drawPile.Where(c => c is CarrierAircraftCard).ToList();
        if (validCards.Count == 0)
            validCards = drawPile.Where(c => c.Type == CardType.Attack).ToList();
        
        // 2. 如果找到了符合条件的牌，进行随机抽取和效果赋予
        if (validCards.Count > 0)
        {
            // 使用战斗内卡牌选择的随机种子抽取一张牌
            var selectedCard = Owner.RunState.Rng.CombatCardSelection.NextItem(validCards)!;
            
            // 设置在本回合内免费打出 (参考 WhiteNoise 的做法)
            selectedCard.SetToFreeThisTurn();
            
            // 将选中的卡牌加入手牌
            await CardPileCmd.Add(selectedCard, PileType.Hand);
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：费用 -1（与原版白噪声一致，升级后 0 费）
        EnergyCost.UpgradeBy(-1);
    }
}