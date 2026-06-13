using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class SlayTheWhales() : TashkentCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5M, ValueProp.Move),
        new RetreatDynamicVar(1M),
        new ChargeDynamicVar(1M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        ArgumentNullException.ThrowIfNull(CombatState);
        
        if (base.IsUpgraded)
        {
            await PowerCmd.Apply<DistancePower>(choiceContext, base.Owner.Creature, -DynamicVars["TashkentSpire2-Retreat"].BaseValue, base.Owner.Creature, this);
        }

        while (true)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            
            var distPower = base.Owner.Creature.GetPower<DistancePower>();
            if (distPower != null && distPower.Amount >= 15m)
            {
                break;
            }
            
            await PowerCmd.Apply<DistancePower>(choiceContext, base.Owner.Creature, DynamicVars["TashkentSpire2-Charge"].BaseValue, base.Owner.Creature, this);
            await PowerCmd.Apply<BackAfterTurnPower>(choiceContext, base.Owner.Creature, DynamicVars["TashkentSpire2-Retreat"].BaseValue, base.Owner.Creature, this);

            bool allInfinite = Owner.Creature.CombatState?.HittableEnemies.All((Creature c) => c.HpDisplay.IsInfinite()) ?? true;
            if (cardPlay.Target.IsDead || allInfinite)
            {
                break;
            }
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1M);
    }
}