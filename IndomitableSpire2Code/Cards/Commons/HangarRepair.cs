using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class HangarRepair() : IndomitableCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 提供消耗关键字提示
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft),
        HoverTipFactory.FromKeyword(IndomitableKeywords.Durability)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获取抽牌堆中所有不满耐久的舰载机牌，条件：是舰载机概念(类/词条/标签) && 拥有耐久机制 && 耐久不满
        var damagedAircraftInDraw = PileType.Draw.GetPile(Owner).Cards
            .Where(c => c.IsCarrierAircraft() && !c.IsFullDurability())
            .ToList();
        
        // 如果没有需要维修的舰载机，直接返回
        if (damagedAircraftInDraw.Count == 0) return;
        
        // 2. 呼出网格选牌界面
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selectedCard = (await CardSelectCmd.FromSimpleGrid(
            choiceContext, damagedAircraftInDraw, Owner, prefs
        )).FirstOrDefault();
        
        // 3. 执行维修并展示，一行代码搞定动画展示和底层数据修改！
        if (selectedCard != null)
            await RepairCmd.FullyRepair(selectedCard);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：移除消耗属性，使其可以在单局游戏中多次循环洗牌并维修
        RemoveKeyword(CardKeyword.Exhaust);
    }
}