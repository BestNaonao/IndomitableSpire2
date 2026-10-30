using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Options;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Commons;

public sealed class Fulmar() : VersatileAircraftCard(1, CardRarity.Common)
{
    private static readonly IReadOnlyList<VersatileCardOption> Options =
        Array.AsReadOnly<VersatileCardOption>([new FulmarFighter(), new FulmarBombs()]);
    
    public override IReadOnlyList<VersatileCardOption> CreateVersatileOptions() => Options;
    
    protected override int MaxDurability { get; set; } = 7;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    protected override (CardTag Tag, CardKeyword Keyword) TypeForOption(string id) => id == FulmarFighter.OptionId
        ? (IndomitableTags.StrikeFighter, IndomitableKeywords.StrikeFighter)
        : (IndomitableTags.DiveBomber, IndomitableKeywords.DiveBomber);
    
    protected override IEnumerable<DynamicVar> AdditionalVars => this.CreateVersatileVars();
}