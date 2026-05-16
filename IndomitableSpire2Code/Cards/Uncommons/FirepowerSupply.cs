using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class FirepowerSupply() : IndomitableCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 【细节拉满】：根据本卡牌是否已升级，动态展示升级或未升级的衍生牌悬浮窗！
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<IncendiaryExpert>(IsUpgraded),
        HoverTipFactory.FromCard<EnhancedApAmmo>(IsUpgraded)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 防呆判定
        if (CombatState == null) return;
        
        // 1. 获取两张衍生牌的模型，并绑定到当前的战斗状态中实例化
        var options = new List<CardModel> 
        {
            CombatState.CreateCard(ModelDb.Card<IncendiaryExpert>(), Owner),
            CombatState.CreateCard(ModelDb.Card<EnhancedApAmmo>(), Owner)
        };
        
        // 2. 如果当前打出的是升级版“火力补给”，则在展示界面前，将选项牌提前升级
        if (IsUpgraded) foreach (var card in options) CardCmd.Upgrade(card);
        
        // 3. 呼出二选一的原生选择界面 (canSkip = false 强制必须选一张)
        var selectedCard = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner);
        if (selectedCard == null) return;
        
        // 4. 将选中的牌加入手牌
        await CardPileCmd.AddGeneratedCardToCombat(selectedCard, PileType.Hand, true);
    }
    
    // 升级逻辑已经在 OnPlay 的 IsUpgraded 判断，以及本地化文本中的 IfUpgraded 标签处理
    protected override void OnUpgrade() { }
}