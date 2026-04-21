using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public sealed class VersatilePerson() : TashkentCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3M, ValueProp.Move),
        new BlockVar(5M, ValueProp.Move),
        new RepeatVar(4),
        new AmmunitionDynamicVar(1M),
        new LoadDynamicVar(2M),
        new AmmuMaxDynamicVar(2M),
    ];
    
    private int _currentAmmu = 1;
    
    [SavedProperty]
    public int CurrentAmmu
    {
        get => _currentAmmu;
        set
        {
            AssertMutable();
            _currentAmmu = value;
            
            base.DynamicVars["TashkentSpire2-Ammu"].BaseValue = _currentAmmu;
        }
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        ArgumentNullException.ThrowIfNull(CombatState);
        
        int ammu = CurrentAmmu;

        if (ammu > 0)
        {
            if (this.CanonicalKeywords.Contains(TashkentKeyword.Barrage))
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .WithHitCount(ammu)
                    .FromCard(this)
                    .TargetingAllOpponents(CombatState)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
                CurrentAmmu = 0;
            }
            else
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .WithHitCount(DynamicVars.Repeat.IntValue)
                    .FromCard(this)
                    .Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
                CurrentAmmu = ammu - 1;
            }
        }
        else
        {
            int load = DynamicVars["TashkentSpire2-Load"].IntValue;

            await Loadcmd.Execute(choiceContext, this, load);
        }
    }
    
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1M);
}