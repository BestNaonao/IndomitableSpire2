using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class SeafireFr47() : CarrierAircraftCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    // 机体稍加固，飞行极度平稳
    protected override int MaxDurability { get; set; } = 6;
    protected override int UpgradeDurabilityAmount { get; set; } = 2;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.StrikeFighter];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.StrikeFighter];
    
    protected override IEnumerable<DynamicVar> AdditionalVars =>
    [
        new DamageVar(4M, ValueProp.Move),
        new RepeatVar(2),
        new CustomPowerVar<VulnerablePower>(2M),
        new ReconVar(3M) // 搭载侦察变量
    ];
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        
        if (cardPlay.Target is { IsAlive: true })
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext: choiceContext, 
                target: cardPlay.Target,
                amount: DynamicVars.Vulnerable.BaseValue,
                applier: Owner.Creature,
                cardSource: this
            );
        
        // 核心机制：呼叫战区侦察
        await ReconCommand.Recon(Owner, DynamicVars.Recon().IntValue).Execute(choiceContext);
        return attackCmd.Results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2M);
        DynamicVars.Recon().UpgradeValueBy(2M); // 升级后侦察深度变强
        UpgradeDurability();
    }
}