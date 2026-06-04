using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class TeaPartyInTheWoods() : IndomitableCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    // 仅限多人模式
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    // 动态悬浮窗：母牌升级后，悬浮窗展示的衍生牌也会变成升级版
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromKeyword(IndomitableKeywords.Require),
        HoverTipFactory.FromCard<Dessert>()
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new MotivationRequireVar(50M),
        new MotivationConsumeVar(20M),
        new CardsVar(2) // 在每个玩家的抽牌堆放入 2 张
    ];
    
    // 核心限制：同时满足“达到需求上限”和“付得起消耗费用”才能打出
    // 如果你在 Extensions 中保留了 ValidateMotivationConditions()，也可以直接调用它
    protected override bool IsPlayable => this.ValidateMotivationConditions();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 消耗干劲
        await this.SpendMotivationCost();
        
        // 2. 为所有玩家发放甜点
        if (CombatState != null)
        {
            foreach (var player in CombatState.Players.Where(p => p.Creature.IsAlive))
            {
                var desserts = CombatState.CreateCards<Dessert>(player, DynamicVars.Cards.IntValue);
                var combat = await CardPileCmd.AddGeneratedCardsToCombat(
                    desserts, PileType.Draw, true, CardPilePosition.Random);
                if (LocalContext.IsMe(player))
                    CardCmd.PreviewCardPileAdd(combat);
            }
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1M);
    }
}