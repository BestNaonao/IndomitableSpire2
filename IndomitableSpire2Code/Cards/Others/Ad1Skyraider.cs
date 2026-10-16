using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

public sealed class Ad1Skyraider() : WhiteEagleAircraftCard(6, TargetType.AnyEnemy)
{
    protected override int MaxDurability { get; set; } = 21;
    protected override int UpgradeDurabilityAmount { get; set; } = 5;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.LevelBomber];
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.LevelBomber];
    protected override IEnumerable<(CardTag Tag, CardKeyword Keyword)> SynergyTypes =>
        [(IndomitableTags.StrikeFighter, IndomitableKeywords.StrikeFighter),
         (IndomitableTags.LevelBomber, IndomitableKeywords.LevelBomber),
         (IndomitableTags.DiveBomber, IndomitableKeywords.DiveBomber)];
    
    protected override IEnumerable<DynamicVar> AdditionalVars => 
    [
        new DamageVar(2M, ValueProp.Move), 
        new RepeatVar(6), 
        new DamageVar("AoeDamage", 10M, ValueProp.Move)
    ];
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        ArgumentNullException.ThrowIfNull(CombatState);
        var focused = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
        var area = await DamageCmd.Attack(DynamicVars["AoeDamage"].BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
        return focused.Results.Concat(area.Results);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(2M);
        DynamicVars["AoeDamage"].UpgradeValueBy(4M);
        UpgradeDurability();
    }
}