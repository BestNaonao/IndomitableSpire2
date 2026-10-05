using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class BedWarming() : IndomitableCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string SpecialLineAudioPath => "res://IndomitableSpire2/sfx/characters/indomitable/main_1_1.wav";
    public override string SpecialLineBanterLocKey => $"{Id.Entry}.banter";
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MotivationConsumeVar(10M),
        new EnergyVar(2),
        new PowerVar<DrowsyPower>(1M)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        EnergyHoverTip,
        HoverTipFactory.FromPower<HypnotizedPower>()
    ];
    
    protected override bool IsPlayable => this.CanAffordMotivationCost();
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Owner.PlayIndomitableCardPresentation(cardPlay, animationTrigger: "Cast", exactDurationSeconds: 16d);
        await this.SpendMotivationCost(choiceContext);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await PowerCmd.Apply<DrowsyPower>(
            choiceContext, 
            Owner.Creature, 
            DynamicVars["DrowsyPower"].BaseValue, 
            Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1M);
    }
}