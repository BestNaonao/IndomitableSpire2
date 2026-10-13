using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Options;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class AviationTechniques() : DurableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self), IVersatileCard
{
    private static readonly IReadOnlyList<VersatileCardOption> Options =
        Array.AsReadOnly<VersatileCardOption>([new EvasiveManeuvers(), new Dynorphin(), new LastGasp()]);
    
    protected override int MaxDurability { get; set; } = 3;
    protected override int UpgradeDurabilityAmount { get; set; } = 0;
    // 与海大黄蜂一样，母卡始终持有全部选项的变量，切换形态不会重建数值。
    protected override IEnumerable<DynamicVar> AdditionalVars => this.CreateVersatileVars();
    
    protected override HashSet<CardTag> CanonicalTags => [IndomitableTags.Versatile];
    
    public CardModel CardModel => this;
    public string? SelectedOptionId { get; private set; }
    string? IVersatileCard.SelectedOptionId
    {
        get => SelectedOptionId;
        set => SelectedOptionId = value;
    }
    
    private int _durabilityCost;
    
    public IReadOnlyList<VersatileCardOption> CreateVersatileOptions() => Options;
    
    public override string Title => this.SelectedOption()?.GetTitle(this) ?? base.Title;
    
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        this.AddVersatileDescriptionArgs(description);
    }
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => this.GetVersatileHoverTips(
        HoverTipFactory.FromKeyword(IndomitableKeywords.Durability),
        HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft));
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        _durabilityCost = 0;
        var option = await VersatileCardCmd.ChooseAndExecute(choiceContext, cardPlay);
        _durabilityCost = option?.GetDurabilityCost(this) ?? 0;
    }
    
    protected override Task<int> CalculateDurabilityLossAfterPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => Task.FromResult(_durabilityCost);
    protected override void CleanupAfterDurabilityLoss() => _durabilityCost = 0;
    
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card == this && Pile?.Type != PileType.Play) VersatileCardCmd.ResetAfterPileChange(this);
        return base.AfterCardChangedPiles(card, oldPileType, clonedBy);
    }
    
    protected override void OnUpgrade() => VersatileCardCmd.UpgradeVersatileOptions(this);
}
