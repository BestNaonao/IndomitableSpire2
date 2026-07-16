using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class Yoga() : IndomitableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 初始提供 2 层瑜伽能力（即 2 点护盾）
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new PowerVar<YogaPower>(2M), 
        new MotivationGainVar(10M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 赋予瑜伽能力
        await PowerCmd.Apply<YogaPower>(
            choiceContext, 
            Owner.Creature, 
            DynamicVars["YogaPower"].BaseValue, 
            Owner.Creature, 
            this
        );
        
        // 获得干劲
        await PowerCmd.Apply<MotivationPower>(
            choiceContext: choiceContext, 
            target: Owner.Creature, 
            amount: DynamicVars.MotivationGain().BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级后变为 3 层
        DynamicVars["YogaPower"].UpgradeValueBy(1M);
        DynamicVars.MotivationGain().UpgradeValueBy(5M);
    }
}