using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class EngageOnSight() : IndomitableCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 默认施加 1 层能力
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<EngageOnSightPower>(1M)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Buff", Owner.Character.CastAnimDelay);
        
        // 施加能力，能力内部的 AfterPowerAmountChanged 钩子会自动检查 this.IsUpgraded
        await PowerCmd.Apply<EngageOnSightPower>(
            choiceContext, 
            Owner.Creature, 
            DynamicVars["EngageOnSightPower"].BaseValue, 
            Owner.Creature, 
            this
        );
    }
}