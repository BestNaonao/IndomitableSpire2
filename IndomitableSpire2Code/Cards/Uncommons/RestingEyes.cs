using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class RestingEyes() : IndomitableCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string SpecialLineAudioPath => "res://IndomitableSpire2/sfx/characters/indomitable/main_1_2.wav";
    public override string SpecialLineBanterLocKey => $"{Id.Entry}.banter";
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Refresh>(IsUpgraded)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is not { } combatState) return;
        await Owner.PlayIndomitableCardPresentation(cardPlay, animationTrigger: "Cast", exactDurationSeconds: 7.0d);
        
        await CardPileCmd.AddGeneratedCardsToCombat(
            combatState.CreateCards<Refresh>(Owner, 1, IsUpgraded), PileType.Hand, Owner);
        await PowerCmd.Apply<RestingEyesPower>(choiceContext, Owner.Creature, 1M, Owner.Creature, this);
    }
}