using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Options;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

// 两种挂载沿用同一母卡模型；重放保持挂载，离开打出区后卸载。
public sealed class FirebrandTF4() : VersatileAircraftCard(2, CardRarity.Uncommon)
{
    private static readonly IReadOnlyList<VersatileCardOption> Options =
        Array.AsReadOnly<VersatileCardOption>([new FirebrandTorpedoes(), new FirebrandBombs()]);
    
    public override IReadOnlyList<VersatileCardOption> CreateVersatileOptions() => Options;
    
    protected override int MaxDurability { get; set; } = 11;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    protected override (CardTag Tag, CardKeyword Keyword) TypeForOption(string id) => id == FirebrandTorpedoes.OptionId
        ? (IndomitableTags.TorpedoBomber, IndomitableKeywords.TorpedoBomber)
        : (IndomitableTags.DiveBomber, IndomitableKeywords.DiveBomber);
    
    // DynamicVars 在克隆时已经初始化。始终注册两套命名变量，切换挂载只选择使用哪一套。
    protected override IEnumerable<DynamicVar> AdditionalVars => [..this.CreateVersatileVars(), new ReconVar(3M)];
    
    // 两种挂载均在攻击结束后侦察，具体挂载与耐久损耗由基类结算。
    protected override async Task AfterLoadout(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await ReconCommand.Recon(Owner, DynamicVars.Recon().IntValue).Execute(choiceContext);
}