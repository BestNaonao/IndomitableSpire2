using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.HoverTips;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Uncommons;

public sealed class Navigator() : IndomitableCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override string SpecialLineAudioPath => "res://IndomitableSpire2/sfx/characters/indomitable/get_2.wav";
    public override string SpecialLineBanterLocKey => $"{Id.Entry}.banter";
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<NavigatorPower>(8M)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
        [CustomHoverTipFactory.FromRecon(), HoverTipFactory.FromPower<MotivationPower>()];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Owner.PlayIndomitableCardPresentation(cardPlay, animationTrigger: "Cast", exactDurationSeconds: 14.7d);
        
        await PowerCmd.Apply<NavigatorPower>(choiceContext, Owner.Creature,
            DynamicVars["NavigatorPower"].BaseValue, Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["NavigatorPower"].UpgradeValueBy(2M);
        AddKeyword(CardKeyword.Retain);
    }
}