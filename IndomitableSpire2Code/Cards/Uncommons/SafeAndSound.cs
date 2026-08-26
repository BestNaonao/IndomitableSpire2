using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class SafeAndSound() : IndomitableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 注册变量：获得 1 点能量
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    
    // 悬浮提示框：优雅和能量图标说明
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromKeyword(IndomitableKeywords.Elegance), 
        HoverTipFactory.ForEnergy(this)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        // 1. 翻倍干劲
        if (Owner.Creature.GetPower<MotivationPower>() is { DisplayAmount: > 0 } motivationPower)
        {
            await PowerCmd.Apply<MotivationPower>(
                choiceContext: choiceContext,
                target: Owner.Creature,
                amount: motivationPower.DisplayAmount, // Add an amount equal to current to double it
                applier: Owner.Creature,
                cardSource: this
            );
        }
        
        // 2. 优雅：获得 1 点能量
        if (Owner.Creature.MeetsElegance()) await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
    
    protected override void OnUpgrade()
    {
        // 升级效果：减少 1 点能量消耗
        EnergyCost.UpgradeBy(-1);
    }
}