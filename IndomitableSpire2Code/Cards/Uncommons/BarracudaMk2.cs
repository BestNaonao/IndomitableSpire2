using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
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
    
    // 添加进水和破甲的提示框
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromPower<FloodingPower>(), HoverTipFactory.FromPower<ArmorBreakPower>()];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ..base.CanonicalVars,
        new DamageVar(8M, ValueProp.Move),
        new PowerVar<FloodingPower>(2M),
        new PowerVar<ArmorBreakPower>(4M) // 基础破甲提升到 4
    ];
    
    protected override async Task<IEnumerable<DamageResult>?> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        var attackCmd = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        
        if (cardPlay.Target is not { IsAlive: true }) return attackCmd.Results;
        
        await PowerCmd.Apply<FloodingPower>(
            target: cardPlay.Target,
            amount: DynamicVars["FloodingPower"].BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
        await PowerCmd.Apply<ArmorBreakPower>(
            target: cardPlay.Target,
            amount: DynamicVars["ArmorBreakPower"].BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
        return attackCmd.Results;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3M);
        DynamicVars["FloodingPower"].UpgradeValueBy(1M);
        DynamicVars["ArmorBreakPower"].UpgradeValueBy(2M);
        UpgradeDurability();
    }
}