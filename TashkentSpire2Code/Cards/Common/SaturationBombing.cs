using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public sealed class SaturationBombing() : AmmunitionCard(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    public override void AfterCreated()
    {
        base.AfterCreated();
        this.BaseReplayCount = 1;
    }
    
    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();
        this.BaseReplayCount = 1; 
    }
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [TashkentKeyword.Barrage];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5M, ValueProp.Move),
        new AmmunitionDynamicVar(0M),
        new LoadDynamicVar(0M),
        new AmmuMaxDynamicVar(6M)
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
        await SaturationBombingcmd.Execute(choiceContext, this.Owner, this);
    }
    
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3M);
}