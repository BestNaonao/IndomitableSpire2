using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public sealed class IlluminationRound() : TashkentCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new MarkDynamicVar(4M),
        new PowerVar<IlluminationRoundPower>(1M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await PowerCmd.Apply<MarkPower>(choiceContext, cardPlay.Target, DynamicVars["TashkentSpire2-Mark"].BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<IlluminationRoundPower>(choiceContext, cardPlay.Target, DynamicVars["IlluminationRoundPower"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Mark"].UpgradeValueBy(2M);
    }
}