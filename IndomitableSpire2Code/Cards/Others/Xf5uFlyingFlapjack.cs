using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

public sealed class Xf5uFlyingFlapjack() : WhiteEagleAircraftCard(4, TargetType.AnyEnemy)
{
    protected override int MaxDurability { get; set; } = 25;
    protected override int UpgradeDurabilityAmount { get; set; } = 7;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [IndomitableKeywords.StrikeFighter, CardKeyword.Sly];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.StrikeFighter];
    protected override IEnumerable<(CardTag Tag, CardKeyword Keyword)> SynergyTypes => [];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];
    
    protected override IEnumerable<DynamicVar> AdditionalVars =>
    [
        new CalculationBaseVar(0M), 
        new ExtraDamageVar(1M), 
        new CalculationExtraVar(1M), 
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            static (card, _) => ((Xf5uFlyingFlapjack)card).DynamicVars.Durability().BaseValue), 
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(
            static (card, _) => Math.Floor(((Xf5uFlyingFlapjack)card).DynamicVars.Durability().BaseValue / 2M))
    ];
    
    public override bool GainsBlock => true;
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var attack = await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        await CreatureCmd.GainBlock(Owner.Creature,
            DynamicVars.CalculatedBlock.Calculate(null), ValueProp.Move, cardPlay);
        return attack.Results;
    }
    
    protected override void OnUpgrade() => UpgradeDurability();
}