using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class MotivationBurst() : IndomitableCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    // 悬停提示：显示该能力的关键词说明
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<MotivationPower>(),
        HoverTipFactory.FromPower<VigorPower>()
    ];
    
    // 注册变量：基础给予 2 层干劲迸发能力
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MotivationBurstPower>(2M)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 播放能力卡专属的施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 2. 将能力施加给自身
        await PowerCmd.Apply<MotivationBurstPower>(
            target: Owner.Creature, 
            amount: DynamicVars["MotivationBurstPower"].BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：单次转化的活力 +1（通过增加能力的层数实现）
        DynamicVars["MotivationBurstPower"].UpgradeValueBy(1M);
    }
}