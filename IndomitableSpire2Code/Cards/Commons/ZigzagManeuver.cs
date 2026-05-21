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
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class ZigzagManeuver() : IndomitableCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 注册变量：5点格挡，2点敏捷
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new MotivationRequireVar(20M),
        new BlockVar(5M, ValueProp.Move),
        new CustomPowerVar<DexterityPower>(2M)
    ];
    
    // 添加“需求”提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromKeyword(IndomitableKeywords.Require)];
    
    // 核心限制：必须有至少 10 点干劲才能打出
    protected override bool IsPlayable => this.MeetsMotivationRequirement();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 播放释放技能的骨骼动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 2. 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        
        // 3. 获得本回合敏捷 (核心：这里施加的是 之字机动能力 临时敏捷)
        await PowerCmd.Apply<ZigzagManeuverPower>(
            target: Owner.Creature, 
            amount: DynamicVars.Dexterity.BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：格挡 +3 (变为8)，敏捷 +1 (变为3)
        DynamicVars.Block.UpgradeValueBy(3M);
        DynamicVars.Dexterity.UpgradeValueBy(1M);
    }
}