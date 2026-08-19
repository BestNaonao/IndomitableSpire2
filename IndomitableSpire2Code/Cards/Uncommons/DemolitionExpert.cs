using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class DemolitionExpert() : IndomitableCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    // 注册变量：能力层数 1
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new PowerVar<DemolitionExpertPower>(1M)];
    
    // 提供悬浮提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromPower<OnFirePower>(), HoverTipFactory.FromPower<FloodingPower>()];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        await PowerCmd.Apply<DemolitionExpertPower>(
            choiceContext: choiceContext, 
            target: Owner.Creature, 
            amount: DynamicVars["DemolitionExpertPower"].BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：层数增加量 +1
        DynamicVars["DemolitionExpertPower"].UpgradeValueBy(1M);
    }
}