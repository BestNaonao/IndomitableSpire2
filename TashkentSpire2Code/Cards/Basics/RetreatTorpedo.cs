using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Basics;

public sealed class RetreatTorpedo() : TashkentCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(18M),
        new RetreatDynamicVar(1M)
    ];
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var temp = new TorpedoPower();
        int value = temp.ComputeTurns();
        (await PowerCmd.Apply<TorpedoPower>(base.Owner.Creature, value, base.Owner.Creature, this))?.SetDamage(base.DynamicVars["TashkentSpire2-Torpedo"].BaseValue);
        await PowerCmd.Apply<DistancePower>(base.Owner.Creature, -base.DynamicVars["TashkentSpire2-Retreat"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}