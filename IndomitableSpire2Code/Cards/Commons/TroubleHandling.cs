using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class TroubleHandling() : IndomitableCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 在悬浮窗中提示“干劲”和“慵懒”状态牌
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Indolent>()];
    
    // 使用 PowerVar 控制干劲获取量
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MotivationGainVar(30M)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 获得干劲
        await PowerCmd.Apply<MotivationPower>(
            target: Owner.Creature,
            amount: DynamicVars.MotivationGain().BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
        
        // 2. 将两张“慵懒”印入弃牌堆
        for (var i = 0; i < 2; ++i)
        {
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(
                    card: CombatState!.CreateCard<Indolent>(Owner), 
                    newPileType: PileType.Discard, 
                    addedByPlayer: true
                )
            );
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后增加 10 点干劲
        DynamicVars.MotivationGain().UpgradeValueBy(10M);
    }
}