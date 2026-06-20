using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class MasterOfLife() : IndomitableCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    // 注册干劲需求变量与能力层数变量
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new MotivationRequireVar(25M),
        new CustomPowerVar<VigorPower>(1M),
        new PowerVar<MasterOfLifePower>(3M)
    ];
    
    // 提供需求的悬浮提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(IndomitableKeywords.Require)];
    
    // 核心限制：干劲不足时不可打出
    protected override bool IsPlayable => this.MeetsMotivationRequirement();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 播放能力卡专属的施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 施加“生活大师”能力
        await PowerCmd.Apply<MasterOfLifePower>(
            choiceContext: choiceContext, 
            target: Owner.Creature,
            amount: DynamicVars["MasterOfLifePower"].BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：每次回血获得的活力 +1（变为 4 层）
        DynamicVars["MasterOfLifePower"].UpgradeValueBy(1M);
    }
}