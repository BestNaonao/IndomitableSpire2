using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

// 继承自 DurableCard，获得内置的耐久度系统支持
public sealed class EvasiveManeuvers() : DurableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 设定最大耐久（即使用次数）为 2
    protected override int MaxDurability { get; set; } = 2;
    protected override int UpgradeDurabilityAmount { get; set; } = 0;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft)];
    
    // 将基础变量与自身的能力变量合并
    protected override IEnumerable<DynamicVar> AdditionalVars => 
        [new PowerVar<EvasiveManeuversPower>(2M)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 1. 施加机动规避能力
        await PowerCmd.Apply<EvasiveManeuversPower>(
            choiceContext: choiceContext, 
            target: Owner.Creature,
            amount: DynamicVars["EvasiveManeuversPower"].BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
        
        // 2. 扣除自身1点耐久（代表消耗了1次使用次数）
        DynamicVars.Durability().BaseValue -= 1;
        
        // 3. 如果耐久耗尽，手动将其送入消耗堆
        if (this.OutOfDurability())
            await CardCmd.Exhaust(choiceContext, this);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：防空免疫次数 +1 (变为 3 次)
        DynamicVars["EvasiveManeuversPower"].UpgradeValueBy(1M);
    }
}