using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class SeaHurricaneTacR() : CarrierAircraftCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override int MaxDurability { get; set; } = 5;
    protected override int UpgradeDurabilityAmount { get; set; } = 2;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.StrikeFighter];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.StrikeFighter];
    
    protected override IEnumerable<DynamicVar> AdditionalVars =>
    [
        new DamageVar(4M, ValueProp.Move),
        new ReconVar(2M),
        new CustomPowerVar<AirRaidGuidancePower>(5M)
    ];
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        
        // 侦察
        await ReconCommand.Recon(Owner, DynamicVars.Recon().IntValue).Execute(choiceContext);
        
        // 赋予“目标引导”能力
        await PowerCmd.Apply<AirRaidGuidancePower>(
            choiceContext, 
            Owner.Creature, 
            DynamicVars.AirRaidGuidance().BaseValue, 
            Owner.Creature, 
            this);
        
        return attackCmd.Results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2M);
        DynamicVars.Recon().UpgradeValueBy(1M);
        DynamicVars.AirRaidGuidance().UpgradeValueBy(2M);
        UpgradeDurability();
    }
}