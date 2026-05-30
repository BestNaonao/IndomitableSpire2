using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class TakeAsBait() : TashkentCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<TakeAsBaitPower>(1M),
        new EnergyVar(1),
        new PowerVar<MarkPreTurnPower>(2M)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MarkPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<TakeAsBaitPower>(base.Owner.Creature, base.DynamicVars["TakeAsBaitPower"].BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<MarkPreTurnPower>(base.Owner.Creature, base.DynamicVars["MarkPreTurnPower"].BaseValue, base.Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["MarkPreTurnPower"].UpgradeValueBy(-1M);
    }
}