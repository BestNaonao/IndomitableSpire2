using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class OxygenTorpedo() : TashkentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(24M),
        new PowerVar<OxygenTorpedoPower>(6M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int turns = TorpedoPower.ComputeTurns(base.Owner.Creature);
        (await PowerCmd.Apply<TorpedoPower>(base.Owner.Creature, (decimal)turns, base.Owner.Creature, this))
            ?.SetDamage(DynamicVars["TashkentSpire2-Torpedo"].BaseValue);
        await PowerCmd.Apply<OxygenTorpedoPower>(base.Owner.Creature, base.DynamicVars["OxygenTorpedoPower"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Torpedo"].UpgradeValueBy(6M);
    }
}