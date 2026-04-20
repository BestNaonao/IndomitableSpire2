using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class RetreatPreparation() : TashkentCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain,
        CardKeyword.Exhaust
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new RetreatDynamicVar(2M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DistancePower>(base.Owner.Creature, -base.DynamicVars["TashkentSpire2-Retreat"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Retreat"].UpgradeValueBy(1M);
    }
}