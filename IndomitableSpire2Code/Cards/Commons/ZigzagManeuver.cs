using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
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
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class ZigzagManeuver() : IndomitableCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    // 注册变量：2点敏捷，4点基础护盾
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new MotivationRequireVar(20M),
        new ShieldVar(4M, ValueProp.Move),
        new CustomPowerVar<DexterityPower>(2M)
    ];
    
    // 添加“需求”提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromKeyword(IndomitableKeywords.Require)];
    
    // 干劲需求由 MotivationRequireVar 决定。
    protected override bool IsPlayable => this.MeetsMotivationRequirement();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 播放释放技能的骨骼动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 2. 先获得本回合敏捷，使随后获得的护盾受其加成。
        await PowerCmd.Apply<ZigzagManeuverPower>(
            choiceContext: choiceContext, 
            target: Owner.Creature, 
            amount: DynamicVars.Dexterity.BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );
        
        // 3. 获得护盾。
        await CustomCreatureCmd.GainShield(choiceContext, Owner.Creature, DynamicVars.Shield(), cardPlay);
    }
    
    protected override void OnUpgrade()
    {
        // 升级增量不变：基础护盾 +3（变为7），敏捷 +1（变为3）。
        DynamicVars.Shield().UpgradeValueBy(3M);
        DynamicVars.Dexterity.UpgradeValueBy(1M);
    }
}