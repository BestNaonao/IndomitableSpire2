using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class SororalUnity() : IndomitableCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => 
        [IndomitableKeywords.Resonance, CardKeyword.Retain, CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        .. HoverTipFactory.FromCardWithCardHoverTips<IllustriousAegis>(IsUpgraded),
        .. HoverTipFactory.FromCardWithCardHoverTips<VictoriousSong>(IsUpgraded),
        .. HoverTipFactory.FromCardWithCardHoverTips<FormidablePressure>(IsUpgraded)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        if (CombatState == null) return;
        
        // 三张姐妹的卡牌各生成一张；入堆前由同心补丁统一继承升级、关键词和附魔。
        CardModel[] cards =
        [
            CombatState.CreateCard(ModelDb.Card<IllustriousAegis>(), Owner),
            CombatState.CreateCard(ModelDb.Card<VictoriousSong>(), Owner),
            CombatState.CreateCard(ModelDb.Card<FormidablePressure>(), Owner)
        ];
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner);
    }
    
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}