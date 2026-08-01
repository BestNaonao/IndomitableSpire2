using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class BayonetTorpedo() : TashkentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(24M),
        new ChargeDynamicVar(2M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DistancePower>(choiceContext, base.Owner.Creature, base.DynamicVars["TashkentSpire2-Charge"].BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<TorpedoPower>(choiceContext, base.Owner.Creature, DynamicVars["TashkentSpire2-Torpedo"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Torpedo"].UpgradeValueBy(12M);
        DynamicVars["TashkentSpire2-Charge"].UpgradeValueBy(1M);
    }
}