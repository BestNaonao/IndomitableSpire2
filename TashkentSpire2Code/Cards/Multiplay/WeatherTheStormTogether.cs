using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Multiplay;

public sealed class WeatherTheStormTogether() : TashkentCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(0M, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        ArgumentNullException.ThrowIfNull(CombatState);
        
        await CreatureCmd.GainBlock(cardPlay.Target, DynamicVars.Block, cardPlay);

        decimal targetBlock = cardPlay.Target.Block;
        
        IEnumerable<Creature> enumerable = from c in base.CombatState.GetTeammatesOf(base.Owner.Creature)
            where c != null && c.IsAlive && c.IsPlayer && c != cardPlay.Target
            select c;
        foreach (Creature ally in enumerable)
        {
            decimal diff = targetBlock - ally.Block;
            if (diff > 0)
            {
                await CreatureCmd.GainBlock(ally, diff, base.DynamicVars.Block.Props, cardPlay);
            }
        }

    }
    
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(8M);
}