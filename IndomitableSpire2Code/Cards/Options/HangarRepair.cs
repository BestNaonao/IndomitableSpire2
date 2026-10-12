using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Options;

/// <summary>保养技巧的机库维修选项；所有数值、拥有者和升级状态均来自母卡。</summary>
public sealed class HangarRepair() : VersatileCardOption(OptionId, durabilityCost: 1)
{
    public const string OptionId = "hangar_repair";
    
    public override IEnumerable<DynamicVar> CreateVars() => [new CardsVar(1)];
    
    public override void Upgrade(CardModel source) => source.DynamicVars.Cards.UpgradeValueBy(1M);
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var candidates = source.Owner.GetCards(sortDrawPile: true, mixPiles: false, PileType.Draw)
            .Where(card => card.IsCarrierAircraft() && !card.IsFullDurability())
            .ToList();
        if (candidates.Count == 0) return;
        // 选择卡牌，进行完全维修
        var prompt = GetSelectionPrompt(source);
        var maxCards = Math.Min(source.DynamicVars.Cards.IntValue, candidates.Count);
        var prefs = new CardSelectorPrefs(prompt, source.IsUpgraded ? 0 : 1, maxCards);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, source.Owner, prefs);
        foreach (var card in selected) await DurabilityCmd.FullyRepair(card);
    }
}