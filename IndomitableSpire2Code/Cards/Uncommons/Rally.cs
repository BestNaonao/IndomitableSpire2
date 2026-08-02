using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Others; // 假设养神 (Refresh) 在这个命名空间
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class Rally() : IndomitableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 注册变量：消耗 10 点干劲，判定阈值为 12
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new MotivationConsumeVar(10M),
        new("Threshold", 12M)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Refresh>()];
    
    // 核心限制：必须有足够的干劲才能打出
    protected override bool IsPlayable => this.CanAffordMotivationCost();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Buff", Owner.Character.CastAnimDelay);
        
        // 1. 扣除干劲
        await this.SpendMotivationCost(choiceContext);
        
        // 2. 在洗牌之前，先获取弃牌堆的卡牌数量
        var discardCount = PileType.Discard.GetPile(Owner).Cards.Count;
        
        // 3. 执行洗牌，将弃牌堆洗入抽牌堆
        await CardPileCmd.Shuffle(choiceContext, Owner);
        
        // 4. 计算应该生成的“养神”数量
        var refreshCount = discardCount / DynamicVars["Threshold"].IntValue;
        
        if (refreshCount > 0 && CombatState != null)
        {
            // 创建指定数量的“养神”卡牌克隆体
            var refreshCards = CombatState.CreateCards<Refresh>(Owner, refreshCount).ToList();
            // 将生成的卡牌放到抽牌堆顶部，并调用引擎的预览动画
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardsToCombat(
                refreshCards, PileType.Draw, Owner, CardPilePosition.Top));
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：费用 -1（变为0费），阈值 -4（变为每8张生成一张）
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Threshold"].UpgradeValueBy(-4M);
    }
}