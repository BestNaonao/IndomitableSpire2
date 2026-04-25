using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Ancient;

public sealed class HeroicShooting() : AmmunitionCard(0, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
{
    public override void AfterCreated()
    {
        base.AfterCreated();
        this.BaseReplayCount = 2;
    }
    
    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();
        this.BaseReplayCount = 2; 
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8M, ValueProp.Move),
        new AmmunitionDynamicVar(0M),
        new LoadDynamicVar(2M),
        new AmmuMaxDynamicVar(9M),
        new MarkDynamicVar(5M)
    ];

    protected override async Task OnPlayWithAmmu(PlayerChoiceContext choiceContext, CardPlay cardPlay, int ammu)
    {
        ArgumentNullException.ThrowIfNull(CombatState);

        if (this.CanonicalKeywords.Contains(TashkentKeyword.Barrage))
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(ammu)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            UpdateAmmuGlobal(0);
        }
        else
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            UpdateAmmuGlobal(ammu - 1);
        }
    }

    protected override async Task OnPlayWithoutAmmu(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);

        int load = DynamicVars["TashkentSpire2-Load"].IntValue;
        await Loadcmd.Execute(choiceContext, this, load);

        await PowerCmd.Apply<MarkPower>(CombatState.HittableEnemies, base.DynamicVars["TashkentSpire2-Mark"].BaseValue, base.Owner.Creature, null);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2M);
        DynamicVars["TashkentSpire2-Mark"].UpgradeValueBy(1M);
    }
}