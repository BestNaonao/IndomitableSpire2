using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class DeckTieDown() : IndomitableCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 提供“保留”关键字的悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];
    
    // 注册变量：最多选择 4 张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(4)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 1. 设置选牌参数：最小 0 张，最大为动态变量 Cards 的值
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue);
        
        // 2. 调出原生的手牌选择界面
        // 过滤条件 (!c.ShouldRetainThisTurn) : 防止玩家选到那些本来就已经自带保留的牌，优化体验
        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext, 
            Owner, 
            prefs, 
            c => !c.ShouldRetainThisTurn, 
            this
        );
        
        // 3. 为选中的牌打上单回合“保留”的标记
        foreach (var card in selectedCards.ToList())
            card.GiveSingleTurnRetain();
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：选择数量 +2（变为最多 6 张）
        DynamicVars.Cards.UpgradeValueBy(2M);
    }
}