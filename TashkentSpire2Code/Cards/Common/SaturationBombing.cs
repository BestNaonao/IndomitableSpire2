using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Commands;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public class SaturationBombing() : TashkentCard(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5M, ValueProp.Move),
        new AmmunitionDynamicVar(0M),
        new LoadDynamicVar(1M),
        new AmmuMaxDynamicVar(2M)
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
        
        int ammu = CurrentAmmu;

        if (ammu > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(DynamicVars.Repeat.IntValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            CurrentAmmu = ammu - 1;
        }
        else
        {
            await SaturationBombingcmd.Execute(choiceContext, this.Owner, this);
        }
    }
    
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2M);
}