using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Others;

[Pool(typeof(StatusCardPool))]
public sealed class Resolve() : IndomitableSpire2Card(1, CardType.Status, CardRarity.Status, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft)];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MotivationGainVar(10M)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MotivationPower>(
            choiceContext: choiceContext,
            target: Owner.Creature,
            amount: DynamicVars.MotivationGain().BaseValue,
            applier: Owner.Creature,
            cardSource: this
        );
        // AllPiles 只包含手牌、抽牌堆、弃牌堆、消耗牌堆和打出牌堆，不包含局外的 Deck。当前正在结算的执念位于打出牌堆，因此也会获得舰载机关键词。
        if (Owner.PlayerCombatState is not { } playerCombatState) return;
        var resolveCards = playerCombatState.AllPiles
            .SelectMany(pile => pile.Cards)
            .OfType<Resolve>()
            .ToList();
        foreach (var resolveCard in resolveCards) CardCmd.ApplyKeyword(resolveCard, IndomitableKeywords.CarrierAircraft);
    }
}
