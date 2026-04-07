using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Basics;

public sealed class TakeABreak() : IndomitableCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MotivationPower>()];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new HealVar(3M),
        new PowerVar<MotivationPower>(15M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 播放一个施法动画（可选，增加视觉反馈）
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 2. 回复生命值
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);

        // 3. 获得干劲
        await PowerCmd.Apply<MotivationPower>(
            target: Owner.Creature, 
            amount: DynamicVars["MotivationPower"].BaseValue, 
            applier: Owner.Creature, 
            cardSource: this
        );

        // 4. 强制结束回合 (参考了 VoidForm 的写法)，第二个参数 false 代表这是不可撤销的操作
        PlayerCmd.EndTurn(Owner, false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Heal.UpgradeValueBy(1M);
        DynamicVars["MotivationPower"].UpgradeValueBy(3M);
    }
}