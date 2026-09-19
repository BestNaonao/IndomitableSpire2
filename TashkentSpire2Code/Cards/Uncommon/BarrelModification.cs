using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class BarrelModification() : TashkentCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        LoadDynamicVar.GetHoverTip()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<BarrelModificationPower>(3M),
        new PowerVar<BarrelModificationLoadPower>(1M),
        new LoadDynamicVar(0M),
        new ShotDynamicVar(0M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<BarrelModificationPower>(choiceContext, base.Owner.Creature, base.DynamicVars["BarrelModificationPower"].BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<BarrelModificationLoadPower>(choiceContext, base.Owner.Creature, base.DynamicVars["BarrelModificationLoadPower"].BaseValue, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["BarrelModificationPower"].UpgradeValueBy(1M);
    }
}
