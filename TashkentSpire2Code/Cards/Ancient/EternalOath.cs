using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Enchantment;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Ancient;

public sealed class EternalOath() : TashkentCard(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    public override void AfterCreated()
    {
        base.AfterCreated();
        CardCmd.Enchant<OathEnchantment>(this, 1m);
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<EternalOathPower>(1M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<EternalOathPower>(base.Owner.Creature, base.DynamicVars["EternalOathPower"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}