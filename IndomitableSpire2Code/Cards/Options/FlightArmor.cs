using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Options;

public sealed class FlightArmor() : VersatileCardOption(OptionId, durabilityCost: 1)
{
    public const string OptionId = "flight_armor";
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var candidates = source.Owner.GetCards(sortDrawPile: true, mixPiles: false, PileType.Hand, PileType.Draw)
            .Where(card => card.IsCarrierAircraft() && card.HasDurability())
            .ToList();
        if (candidates.Count == 0) return;
        // 选择一张牌提高最大耐久度
        var prefs = new CardSelectorPrefs(GetSelectionPrompt(source), 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, candidates, source.Owner, prefs);
        foreach (var card in selected)
            await DurabilityCmd.GainMaxDurability(card, source.DynamicVars["DurabilityGain"].IntValue);
    }
}