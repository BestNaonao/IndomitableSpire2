using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
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

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class BarracudaMk2() : CarrierAircraftCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 全金属重型轰炸机结构，耐久较高
    protected override int MaxDurability { get; set; } = 9;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.TorpedoBomber];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.TorpedoBomber];
    
    // 使用 CustomPowerVar 作为能力的动态变量
    protected override IEnumerable<DynamicVar> AdditionalVars =>
    [
        new DamageVar(8M, ValueProp.Move),
        new CustomPowerVar<FloodingPower>(2M),
        new CustomPowerVar<ArmorBreakPower>(4M) // 基础破甲提升到 4
    ];
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        
        if (cardPlay.Target is not { IsAlive: true }) return attackCmd.Results;
        
        await PowerCmd.Apply<FloodingPower>(
            choiceContext: choiceContext, 
            target: cardPlay.Target,
            amount: DynamicVars.Flooding().BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
        await PowerCmd.Apply<ArmorBreakPower>(
            choiceContext: choiceContext, 
            target: cardPlay.Target,
            amount: DynamicVars.ArmorBreak().BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
        return attackCmd.Results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3M);
        DynamicVars.Flooding().UpgradeValueBy(1M);
        DynamicVars.ArmorBreak().UpgradeValueBy(2M);
        UpgradeDurability();
    }
}