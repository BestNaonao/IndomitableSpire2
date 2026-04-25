using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
        if (base.IsUpgraded)
        {
            await PowerCmd.Apply<DistancePower>(base.Owner.Creature, -DynamicVars["TashkentSpire2-Retreat"].BaseValue, base.Owner.Creature, null);
        }
        
        do
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            await PowerCmd.Apply<DistancePower>(base.Owner!.Creature, DynamicVars["TashkentSpire2-Charge"].BaseValue, base.Owner.Creature, null);
        }
        while ((int)(base.Owner?.Creature.GetPower<DistancePower>()?.Amount ?? 0m) < 5);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1M);
    }
}