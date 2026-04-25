using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public sealed class ArtilleryStrike() : AmmunitionCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    
    public override void AfterCreated()
    {
        base.AfterCreated();
        this.BaseReplayCount = 3;
    }
    
    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();
        this.BaseReplayCount = 3; 
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3M, ValueProp.Move),
        new AmmunitionDynamicVar(3M),
        new LoadDynamicVar(3M),
        new AmmuMaxDynamicVar(6M),
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
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            UpdateAmmuGlobal(ammu - 1);
        }
    }

    protected override async Task OnPlayWithoutAmmu(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int load = DynamicVars["TashkentSpire2-Load"].IntValue;
        await Loadcmd.Execute(choiceContext, this, load);
    }
    
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1M);
}