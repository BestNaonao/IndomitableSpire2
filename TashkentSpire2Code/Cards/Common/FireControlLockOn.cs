using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;
using TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public sealed class FireControlLockOn() : TashkentCard(2, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new MarkDynamicVar(7M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        NFireControlRadarVfx.Play(this, cardPlay.Target);
        await PowerCmd.Apply<MarkPower>(choiceContext, cardPlay.Target, base.DynamicVars["TashkentSpire2-Mark"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Mark"].UpgradeValueBy(3M);
    }
}
