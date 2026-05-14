using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class HangarRepair() : IndomitableCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 提供消耗关键字提示
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获取抽牌堆中所有不满耐久的舰载机牌
        var damagedAircraftInDraw = PileType.Draw.GetPile(Owner).Cards
            .OfType<CarrierAircraftCard>()
            .Where(c => !c.IsFullDurability())
            .ToList();
        
        // 如果没有需要维修的舰载机，直接返回
        if (damagedAircraftInDraw.Count == 0) return;
        
        // 2. 呼出网格选牌界面
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selectedCard = (await CardSelectCmd.FromSimpleGrid(
            choiceContext, 
            damagedAircraftInDraw, 
            Owner, 
            prefs
        )).FirstOrDefault();
        
        // 3. 执行维修并展示
        if (selectedCard is CarrierAircraftCard aircraft)
        {
            aircraft.FullyRepair();
            // 在屏幕中央闪烁展示被修复的卡牌，给予强烈的正反馈
            CardCmd.Preview(aircraft);
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：移除消耗属性，使其可以在单局游戏中多次循环洗牌并维修
        RemoveKeyword(CardKeyword.Exhaust);
    }
}