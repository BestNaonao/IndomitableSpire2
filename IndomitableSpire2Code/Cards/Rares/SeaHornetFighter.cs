using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Options;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

// 保留原 F.20 的模型 ID；两种挂载都是同一模型的临时形态。
public sealed class SeaHornetFighter() : VersatileAircraftCard(2, CardRarity.Rare)
{
    private static readonly IReadOnlyList<VersatileCardOption> Options =
        Array.AsReadOnly<VersatileCardOption>([new SeaHornetRockets(), new SeaHornetBombs()]);
    
    public override IReadOnlyList<VersatileCardOption> CreateVersatileOptions() => Options;
    
    protected override int MaxDurability { get; set; } = 15;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    // 编队独立于挂载；切换、卸载或降级时始终保留。
    public override IEnumerable<CardKeyword> CanonicalKeywords => [..base.CanonicalKeywords, IndomitableKeywords.Formation];
    
    protected override (CardTag Tag, CardKeyword Keyword) TypeForOption(string id) => id == SeaHornetRockets.OptionId
        ? (IndomitableTags.StrikeFighter, IndomitableKeywords.StrikeFighter)
        : (IndomitableTags.LevelBomber, IndomitableKeywords.LevelBomber);
    
    // DynamicVars 在克隆时已经初始化。始终注册两套命名变量，切换挂载只选择使用哪一套。
    protected override IEnumerable<DynamicVar> AdditionalVars => this.CreateVersatileVars();
}