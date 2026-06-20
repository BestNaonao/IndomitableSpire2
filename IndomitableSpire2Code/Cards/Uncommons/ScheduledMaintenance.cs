using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class ScheduledMaintenance() : IndomitableCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    // 注册能力变量：基础 4 层
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new PowerVar<ScheduledMaintenancePower>(4M)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 播放能力卡专属的施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 施加定期检修能力
        await PowerCmd.Apply<ScheduledMaintenancePower>(
            choiceContext: choiceContext, 
            target: Owner.Creature,
            amount: DynamicVars["ScheduledMaintenancePower"].BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：层数 +2（总计提供 6 层修复力）
        DynamicVars["ScheduledMaintenancePower"].UpgradeValueBy(2M);
    }
}