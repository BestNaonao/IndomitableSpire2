using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class LadysWhim() : IndomitableCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<LadysWhimPower>(2M)];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<MotivationPower>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放台词、语音和动画，稍后执行逻辑
        await Owner.PlayIndomitableCardPresentation(cardPlay, Id.Entry, VfxColor.Gold, 
            "res://IndomitableSpire2/sfx/characters/indomitable/main_6_ex1100.wav", 
            animationTrigger: "Cast", exactDurationSeconds: 8.8d);
        
        await PowerCmd.Apply<LadysWhimPower>(
            choiceContext, 
            Owner.Creature, 
            DynamicVars["LadysWhimPower"].BaseValue, 
            Owner.Creature, 
            this);
    }
    
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
        AddKeyword(CardKeyword.Innate);
    }
}