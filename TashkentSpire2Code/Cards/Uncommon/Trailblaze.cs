using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class Trailblaze() : TashkentCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(4m),
        new ExtraDamageVar(2m),
        new ChargeDynamicVar(1m),
        new PowerVar<BackAfterTurnPower>(1m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) => 
        {
            var distPower = card.Owner.Creature.GetPower<DistancePower>();
            if (distPower != null)
            {
                return (decimal)distPower.TotalIncreasedAmount;
            }
            return 0m;
        })
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<BackAfterTurnPower>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await DamageCmd.Attack(base.DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        await PowerCmd.Apply<DistancePower>(choiceContext, base.Owner.Creature, base.DynamicVars["TashkentSpire2-Charge"].BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<BackAfterTurnPower>(choiceContext, base.Owner.Creature, base.DynamicVars["BackAfterTurnPower"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(1M);
}