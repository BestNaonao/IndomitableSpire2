using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Options;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class MaintenanceTechniques() : DurableCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self), IVersatileCard
{
    private static readonly IReadOnlyList<VersatileCardOption> Options =
        Array.AsReadOnly<VersatileCardOption>([new HangarRepair(), new FlightArmor(), new ScheduledMaintenance()]);
    
    protected override int MaxDurability { get; set; } = 3;
    protected override int UpgradeDurabilityAmount { get; set; } = 0;
    // 与海大黄蜂一样，母卡始终持有全部选项的变量，切换形态不会重建数值。
    protected override IEnumerable<DynamicVar> AdditionalVars => 
    [
        new CardsVar(1), 
        new("DurabilityGain", 4M), 
        new PowerVar<ScheduledMaintenancePower>(3M)
    ];
    
    protected override HashSet<CardTag> CanonicalTags => [IndomitableTags.Versatile];
    
    public string? SelectedOptionId { get; private set; }
    private VersatileCardOption? SelectedOption => Options.FirstOrDefault(option => option.Id == SelectedOptionId);
    private int _durabilityCost;
    
    public IReadOnlyList<VersatileCardOption> CreateVersatileOptions() => Options;
    
    public void SelectVersatileOption(string? optionId)
    {
        AssertMutable();
        if (optionId != null && Options.All(option => option.Id != optionId))
            throw new ArgumentException("Unknown maintenance option.", nameof(optionId));
        SelectedOptionId = optionId;
    }
    
    public override string Title => SelectedOption?.GetTitle(this) ?? base.Title;
    
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("HasOption", SelectedOption != null);
        SelectedOption?.AddDescriptionArgs(this, description);
    }
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(IndomitableKeywords.Versatile);
            yield return HoverTipFactory.FromKeyword(IndomitableKeywords.Durability);
            yield return HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft);
            if (SelectedOption != null) yield break;
            foreach (var option in Options) yield return option.CreateHoverTip(this);
        }
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        _durabilityCost = 0;
        var option = await VersatileCardCmd.ChooseAndExecute(choiceContext, cardPlay);
        _durabilityCost = option?.DurabilityCost ?? 0;
    }
    
    protected override Task<int> CalculateDurabilityLossAfterPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => Task.FromResult(_durabilityCost);
    protected override void CleanupAfterDurabilityLoss() => _durabilityCost = 0;
    
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card == this && Pile?.Type != PileType.Play && SelectedOptionId != null)
        {
            SelectVersatileOption(null);
            NCard.FindOnTable(this)?.UpdateVisuals(Pile?.Type ?? PileType.None, CardPreviewMode.Normal);
        }
        return base.AfterCardChangedPiles(card, oldPileType, clonedBy);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1M);
        DynamicVars["DurabilityGain"].UpgradeValueBy(2M);
        DynamicVars["ScheduledMaintenancePower"].UpgradeValueBy(2M);
    }
}