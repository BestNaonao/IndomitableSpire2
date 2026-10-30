using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class SeafangMk32() : CarrierAircraftCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private int _permanentDurabilityGain;
    // 只保存永久增量。读档恢复增量后，原版再重放升级，避免把升级的 +1 重复计算。
    [SavedProperty]
    public int PermanentDurabilityGain
    {
        get => _permanentDurabilityGain;
        set
        {
            AssertMutable();
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            var delta = value - _permanentDurabilityGain;
            _permanentDurabilityGain = value;
            if (delta >= 0)
            {
                var gained = GainMaxDurabilityInternal(delta);
                GainDurabilityInternal(gained);
            }
            else LoseMaxDurabilityInternal(-delta);
        }
    }
    
    protected override int MaxDurability { get; set; } = 10;
    protected override int UpgradeDurabilityAmount { get; set; } = 2;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [IndomitableKeywords.StrikeFighter];
    
    protected override IEnumerable<CardTag> SubclassTags => [IndomitableTags.StrikeFighter];
    
    protected override IEnumerable<DynamicVar> AdditionalVars =>
    [
        new CalculationBaseVar(0M), new ExtraDamageVar(1M),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(static (card, _) => card.DynamicVars.Durability().BaseValue),
        new IncreaseVar(3M)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Fatal)];
    
    protected override async Task<IEnumerable<IEnumerable<DamageResult>>> OnAircraftPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        
        var canTriggerFatal = cardPlay.Target.Powers.All(power => power.ShouldOwnerDeathTriggerFatal());
        var attack = await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .ExecuteWithAircraftFlight(choiceContext);
        
        if (canTriggerFatal && attack.Results.SelectMany(hit => hit).Any(result => result.WasTargetKilled))
        {
            var gain = DynamicVars.Increase().IntValue;
            await GrowPermanently(gain, true);
            if (DeckVersion is SeafangMk32 deck && deck != this) await deck.GrowPermanently(gain, false);
        }
        return attack.Results;
    }
    
    private async Task GrowPermanently(int amount, bool skipVisuals)
    {
        var before = DynamicVars.MaxDurability().IntValue;
        await DurabilityCmd.GainMaxDurability(this, amount, skipVisuals: skipVisuals);
        _permanentDurabilityGain += DynamicVars.MaxDurability().IntValue - before; // 没走 PermanentDurabilityGain.set
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Increase().UpgradeValueBy(1M);
        UpgradeDurability();
    }
}