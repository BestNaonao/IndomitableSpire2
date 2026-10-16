using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

public sealed class Xtb2d1Skypirate() : WhiteEagleAircraftCard(8, TargetType.AnyEnemy)
{
    protected override int MaxDurability { get; set; } = 24;
    protected override int UpgradeDurabilityAmount { get; set; } = 6;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.TorpedoBomber];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.TorpedoBomber];
    protected override IEnumerable<(CardTag Tag, CardKeyword Keyword)> SynergyTypes =>
        [(IndomitableTags.StrikeFighter, IndomitableKeywords.StrikeFighter),
         (IndomitableTags.TorpedoBomber, IndomitableKeywords.TorpedoBomber)];
    
    protected override IEnumerable<DynamicVar> AdditionalVars => 
    [
        new DamageVar(8M, ValueProp.Move | ValueProp.Unblockable), 
        new RepeatVar(4), 
        new CustomPowerVar<FloodingPower>(1M)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Append(HoverTipFactory.FromPower<FloodingPower>());
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var results = new List<IEnumerable<DamageResult>>();
        for (var hit = 0; hit < DynamicVars.Repeat.IntValue && cardPlay.Target.IsAlive; hit++)
            results.Add(await CreatureCmd.Damage(
                choiceContext, cardPlay.Target, DynamicVars.Damage, Owner.Creature, this, cardPlay));
        
        if (cardPlay.Target.IsAlive)
            await PowerCmd.Apply<FloodingPower>(choiceContext, cardPlay.Target,
                DynamicVars.Flooding().BaseValue, Owner.Creature, this);
        return results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3M);
        UpgradeDurability();
    }
}