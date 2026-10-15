using BaseLib.Patches.Hooks;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class HomeFlagship() : IndomitableCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string SpecialLineAudioPath => "res://IndomitableSpire2/sfx/characters/indomitable/warcry.wav";
    public override string SpecialLineBanterLocKey => $"{Id.Entry}.banter";
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ShieldVar(2M, ValueProp.Move)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(IndomitableKeywords.CarrierAircraft)];
    
    public override bool GainsBlock => true;
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Owner.PlayIndomitableCardPresentation(cardPlay, animationTrigger: "Cast", exactDurationSeconds: 6.6d);
        
        var hand = PileType.Hand.GetPile(Owner);
        await CardCmd.Discard(choiceContext, hand.Cards.Where(card => card.IsCarrierAircraft()).ToList());
        await Cmd.CustomScaledWait(0f, 0.25f);
        
        // 弃牌触发的抽牌等效果结算后，再按玩家的实际手牌上限填满；随机结果允许重复。
        var count = Math.Max(0, MaxHandSizePatch.GetMaxHandSize(Owner, CardPile.MaxCardsInHand) - hand.Cards.Count);
        var cards = CardFactory.GetForCombat(Owner, ModelDb.AllCards.Where(card => card.IsCarrierAircraft()), 
            count, Owner.RunState.Rng.CombatCardGeneration).ToList();
        foreach (var card in cards.Where(_ => IsUpgraded)) CardCmd.Upgrade(card);
        // 先完成整组卡牌的入手与生成钩子，再按批量结果结算护盾。
        var results = await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner);
        foreach (var result in results)
        {
            // 满手溢出或后续效果移走的牌不计入实际留在手牌的数量。如果必要，也可以不校验是否成功入手。
            if (result is { success: true, cardAdded.Pile.Type: PileType.Hand })
                await CustomCreatureCmd.GainShield(choiceContext, Owner.Creature, DynamicVars.Shield(), cardPlay);
        }
    }
    
    protected override void OnUpgrade() => DynamicVars.Shield().UpgradeValueBy(1M);
}