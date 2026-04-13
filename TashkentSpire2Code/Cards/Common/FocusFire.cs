using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Commands;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public class FocusFire() : TashkentCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(2M, ValueProp.Move),
        new AmmunitionDynamicVar(1M),
        new LoadDynamicVar(1M),
        new AmmuMaxDynamicVar(1M),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier((CardModel card, Creature? _) =>
            {
                return card.Owner.PlayerCombatState.AllCards.Sum(c =>
                {
                    if (c.DynamicVars.TryGetValue("TashkentSpire2-Ammu", out var ammuVar))
                        return ammuVar.IntValue;
                    return 0;
                });
            })
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
                .WithHitCount(DynamicVars.CalculatedDamage.IntValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            CurrentAmmu = ammu - 1;
        }
        else
        {
            int load = DynamicVars["TashkentSpire2-Load"].IntValue;

            await Loadcmd.Execute(choiceContext, this, load);
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1M);
        DynamicVars["TashkentSpire2-Ammu"].UpgradeValueBy(1M);
        DynamicVars["TashkentSpire2-Ammu-Max"].UpgradeValueBy(1M);
    }
}