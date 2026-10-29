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

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class Firecrest() : CarrierAircraftCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override int MaxDurability { get; set; } = 10;
    protected override int UpgradeDurabilityAmount { get; set; } = 0;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => 
        [IndomitableKeywords.TorpedoBomber, IndomitableKeywords.Afterglow];
    
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.TorpedoBomber];
    
    protected override IEnumerable<DynamicVar> AdditionalVars => 
    [
        new DamageVar(9M, ValueProp.Move), 
        new CustomPowerVar<FloodingPower>(2M), 
        new DurabilityCostVar(2M)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<OnFirePower>(), HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    
    protected override bool ShouldGlowGoldInternal => CombatState?.HittableEnemies.Any(e => e.HasPower<OnFirePower>()) ?? false;
    
    private int _extraDurabilityLoss;
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // 与拆卸一样，在攻击开始前判断条件；击杀或攻击后移除起火都不会改变这次附加消耗。
        var burning = cardPlay.Target.HasPower<OnFirePower>();
        _extraDurabilityLoss = burning ? DynamicVars.DurabilityCost().IntValue : 0;
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .ExecuteWithAircraftFlight(choiceContext);
        if (cardPlay.Target.IsAlive)
            await PowerCmd.Apply<FloodingPower>(choiceContext, cardPlay.Target,
                DynamicVars.Flooding().BaseValue * (burning ? 2 : 1), Owner.Creature, this);
        return attack.Results;
    }
    
    // 根据之前 Onplay 里的计算结果增加额外损失。
    protected override async Task<int> CalculateDurabilityLossAfterPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await base.CalculateDurabilityLossAfterPlay(choiceContext, cardPlay) + _extraDurabilityLoss;
    
    public override int CalculateDurabilityLossForPreview(IEnumerable<Creature> targets)
    {
        var list = targets.ToList();
        return base.CalculateDurabilityLossForPreview(list) +
            (list.Any(c => c.HasPower<OnFirePower>()) ? DynamicVars.DurabilityCost().IntValue : 0);
    }
    
    protected override void CleanupAfterDurabilityLoss()
    {
        base.CleanupAfterDurabilityLoss();
        _extraDurabilityLoss = 0;
    }
    
    protected override void AfterCloned()
    {
        base.AfterCloned();
        _extraDurabilityLoss = 0;
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3M);
        DynamicVars.Flooding().UpgradeValueBy(1M);
    }
}