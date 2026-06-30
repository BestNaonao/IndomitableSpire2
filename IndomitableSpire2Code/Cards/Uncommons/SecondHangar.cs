using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class SecondHangar() : IndomitableCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    // 注册能力变量：初始提供 3 层“第二机库”
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SecondHangarPower>(3M)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 赋予玩家“第二机库”能力
        await PowerCmd.Apply<SecondHangarPower>(
            choiceContext: choiceContext, 
            target: Owner.Creature, 
            amount: DynamicVars["SecondHangarPower"].BaseValue, 
            applier: Owner.Creature, 
            cardSource: this);
    }
    
    protected override void OnUpgrade()
    {
        // 升级后：增加 1 层，即总共增加 4 张手牌上限
        DynamicVars["SecondHangarPower"].UpgradeValueBy(1M);
    }
}