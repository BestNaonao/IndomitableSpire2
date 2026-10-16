using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

[Pool(typeof(EventCardPool))]
public abstract class WhiteEagleAircraftCard(int cost, TargetType target)
    : CarrierAircraftCard(cost, CardType.Attack, CardRarity.Event, target)
{
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<IndomitableCardPool>();
    
    // 一张已打出的牌即使同时属于两个指定机种，也只减少一次费用。
    protected abstract IEnumerable<(CardTag Tag, CardKeyword Keyword)> SynergyTypes { get; }
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(IndomitableKeywords.TypeSynergy)];
    
    private bool MatchesSynergy(CardModel card) =>
        card.IsCarrierAircraft() && SynergyTypes.Any(type => card.IsAircraftType(type.Tag, type.Keyword));
    
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card != this || IsClone) return Task.CompletedTask;
        var previousPlays = CombatManager.Instance.History.Entries
            .OfType<CardPlayFinishedEntry>()
            .Count(entry => MatchesSynergy(entry.CardPlay.Card));
        EnergyCost.AddThisCombat(-previousPlays);
        return Task.CompletedTask;
    }
    
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (MatchesSynergy(cardPlay.Card)) EnergyCost.AddThisCombat(-1);
        return base.AfterCardPlayed(choiceContext, cardPlay);
    }
}