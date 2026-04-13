using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public class OxygenTorpedo() : TashkentCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(60M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var temp = new TorpedoPower();
        int value = temp.ComputeTurns();
        (await PowerCmd.Apply<TorpedoPower>(base.Owner.Creature, value, base.Owner.Creature, this))?.SetDamage(base.DynamicVars["TashkentSpire2-Torpedo"].BaseValue);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Torpedo"].UpgradeValueBy(18M);
    }
}