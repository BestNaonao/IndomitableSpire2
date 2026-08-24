using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Ancients;

/// <summary>
/// 全神贯注：在干劲充满后解锁“认真模式”按钮。
/// </summary>
public sealed class FullConcentration() : IndomitableCard(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FullConcentrationPower>(1M)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MotivationPower>(),
        HoverTipFactory.FromPower<EarnestModePower>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<FullConcentrationPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["FullConcentrationPower"].BaseValue,
            Owner.Creature,
            this);
    }
    
    protected override void OnUpgrade()
    {
        // 与给出的“永恒誓约”先古能力牌一致：升级后费用从 2 降为 1。
        EnergyCost.UpgradeBy(-1);
    }
}