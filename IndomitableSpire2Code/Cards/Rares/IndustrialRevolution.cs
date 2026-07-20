using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class IndustrialRevolution() : IndomitableCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 注册变量：施加 1 层“工业革命”能力（每次触发提供1个待触发的重放名额）
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<IndustrialRevolutionPower>(1M)];
    
    // 悬浮提示框：展示原版游戏内置的“重放”静态提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        (IsUpgraded ? [HoverTipFactory.FromKeyword(CardKeyword.Retain)] : Array.Empty<IHoverTip>())
        .Append(HoverTipFactory.Static(StaticHoverTip.ReplayStatic));
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        await PowerCmd.Apply<IndustrialRevolutionPower>(
            choiceContext: choiceContext, 
            target: Owner.Creature,
            amount: DynamicVars["IndustrialRevolutionPower"].BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：添加保留关键词
        AddKeyword(CardKeyword.Retain);
    }
}