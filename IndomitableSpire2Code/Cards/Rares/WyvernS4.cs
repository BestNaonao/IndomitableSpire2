using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Options;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class WyvernS4() : VersatileAircraftCard(2, CardRarity.Rare)
{
    private static readonly IReadOnlyList<VersatileCardOption> Options =
        Array.AsReadOnly<VersatileCardOption>([new WyvernFighter(), new WyvernTorpedoes()]);
    
    public override IReadOnlyList<VersatileCardOption> CreateVersatileOptions() => Options;
    
    protected override int MaxDurability { get; set; } = 13;
    protected override int UpgradeDurabilityAmount { get; set; } = 3;
    
    protected override (CardTag Tag, CardKeyword Keyword) TypeForOption(string id) => id == WyvernFighter.OptionId
        ? (IndomitableTags.StrikeFighter, IndomitableKeywords.StrikeFighter)
        : (IndomitableTags.TorpedoBomber, IndomitableKeywords.TorpedoBomber);
    
    protected override IEnumerable<DynamicVar> AdditionalVars => this.CreateVersatileVars();
    
    private int _aloftAutoPlayDepth;
    
    internal async Task AutoPlayAloft(PlayerChoiceContext context, Creature target)
    {
        _aloftAutoPlayDepth++;
        try { await CardCmd.AutoPlay(context, this, target); }
        finally
        {
            _aloftAutoPlayDepth--;
            // 被阻止打出、异常或结束战斗也不能留下强制挂载；通常已由离堆钩子卸载。
            if (_aloftAutoPlayDepth == 0) this.SelectVersatileOption(null);
        }
    }
    
    protected override Task BeforeLoadout(PlayerChoiceContext context, CardPlay play)
    {
        // 每个 CardPlay 都执行，包括重放；普通 AutoPlay 不进入此作用域，仍正常选择挂载。
        if (_aloftAutoPlayDepth > 0 && play.IsAutoPlay) this.SelectVersatileOption(WyvernFighter.OptionId);
        return Task.CompletedTask;
    }
    
    public override async Task AfterCardPlayedLate(PlayerChoiceContext context, CardPlay play)
    {
        await base.AfterCardPlayedLate(context, play);
        // 原版在选择挂载前就计算最终去向，故不能仅覆写 GetResultLocationForCardPlay。
        // 等最后一次重放和耐久结算完成后移动；保留消耗、移出战斗等优先去向。
        if (play.Card == this && play.IsLastInSeries && SelectedOptionId == WyvernFighter.OptionId &&
            Pile?.Type == PileType.Play && play.ResultPile is not (PileType.Exhaust or PileType.None) && 
            !this.OutOfDurability())
            await CardPileCmd.Add(this, PileType.Draw, CardPilePosition.Bottom);
    }
    
    protected override void AfterCloned()
    {
        base.AfterCloned();
        _aloftAutoPlayDepth = 0;
    }
}