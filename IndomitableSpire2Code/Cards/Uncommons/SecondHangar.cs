using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class SecondHangar() : IndomitableCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private const string BlockLossKey = "BlockLoss";
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new(BlockLossKey, 3M),
        new PowerVar<SecondHangarPower>(4M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 先移除格挡；CreatureCmd.LoseBlock 会将实际损失同步给护盾。
        await CreatureCmd.LoseBlock(
            choiceContext,
            Owner.Creature,
            DynamicVars[BlockLossKey].BaseValue,
            Owner.Creature);
        
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
        // 升级后：格挡损失由 3 降至 2，能力由 4 层提升至 6 层。
        DynamicVars["SecondHangarPower"].UpgradeValueBy(2M);
        DynamicVars[BlockLossKey].UpgradeValueBy(-1M);
    }
}