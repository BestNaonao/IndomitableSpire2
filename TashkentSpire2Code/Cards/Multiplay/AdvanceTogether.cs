using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Multiplay;

public sealed class AdvanceTogether() : TashkentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new ChargeDynamicVar(3m),
        new PowerVar<BackAfterTurnPower>(3m)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BackAfterTurnPower>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        IEnumerable<Creature> enumerable = from c in base.CombatState.GetTeammatesOf(base.Owner.Creature)
            where c != null && c.IsAlive && c.IsPlayer
            select c;
        foreach (Creature item in enumerable)
        {
            await PowerCmd.Apply<DistancePower>(choiceContext, item, base.DynamicVars["TashkentSpire2-Charge"].BaseValue, base.Owner.Creature, this);
            await PowerCmd.Apply<BackAfterTurnPower>(choiceContext, item, base.DynamicVars["BackAfterTurnPower"].BaseValue, base.Owner.Creature, this);
        }
    }
    
    protected override void OnUpgrade()
    {
        base.DynamicVars["TashkentSpire2-Charge"].UpgradeValueBy(2M);
        base.DynamicVars["BackAfterTurnPower"].UpgradeValueBy(2M);
    }
}