using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class Furious() : IndomitableCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    // 注册变量：抽 1 张牌，固定 4 层起火
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new CardsVar(1),
        new CustomPowerVar<OnFirePower>(4M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 1. 先给予自身“怒火中烧”能力
        await PowerCmd.Apply<FuriousPower>(
            choiceContext: choiceContext, 
            target: Owner.Creature, 
            amount: DynamicVars.Cards.BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );
        
        // 2. 给予自身 4 层起火。由于刚刚获得了能力，这一步会立刻触发第一抽！
        await PowerCmd.Apply<OnFirePower>(
            choiceContext: choiceContext, 
            target: Owner.Creature, 
            amount: DynamicVars.OnFire().BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：抽牌数量 +1
        DynamicVars.Cards.UpgradeValueBy(1M);
    }
}