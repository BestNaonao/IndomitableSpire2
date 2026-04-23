using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class UltimateWeapon() : AmmunitionCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AmmunitionDynamicVar(5M),
        new LoadDynamicVar(5M),
        new AmmuMaxDynamicVar(99M),
        new CalculationBaseVar(10M),
        new ExtraDamageVar(2M),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier((CardModel card, Creature? _) =>
            {
                if (card is IAmmunitionCard ammuCard)
                {
                    return ammuCard.CurrentAmmu;
                }
                return 0;
            })
    ];

    protected override async Task OnPlayWithAmmu(PlayerChoiceContext choiceContext, CardPlay cardPlay, int ammu)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        
        if (this.CanonicalKeywords.Contains(TashkentKeyword.Barrage))
        {
            await DamageCmd.Attack(base.DynamicVars.CalculatedDamage)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            UpdateAmmuGlobal(ammu - 1);
        }
        else
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
            await DamageCmd.Attack(base.DynamicVars.CalculatedDamage)
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

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}