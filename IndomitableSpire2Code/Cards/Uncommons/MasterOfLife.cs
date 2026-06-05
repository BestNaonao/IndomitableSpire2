using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class MasterOfLife() : IndomitableCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    // 注册能力层数变量：基础施加 3 层生活大师能力（即每次回血得 3 活力）
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MasterOfLifePower>(3M)];
    
    // 提供活力的悬浮提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VigorPower>()];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 播放能力卡专属的施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 施加“生活大师”能力
        await PowerCmd.Apply<MasterOfLifePower>(
            target: Owner.Creature,
            amount: DynamicVars["MasterOfLifePower"].BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：每次回血获得的活力 +1（变为 4 层）
        DynamicVars["MasterOfLifePower"].UpgradeValueBy(1M);
    }
}