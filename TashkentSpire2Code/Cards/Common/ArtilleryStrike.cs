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
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3M, ValueProp.Move),
        new RepeatVar(4),
        new AmmunitionDynamicVar(1M),
        new LoadDynamicVar(2M),
        new AmmuMaxDynamicVar(2M),
    ];
    
    protected override async Task OnPlayWithAmmu(PlayerChoiceContext choiceContext, CardPlay cardPlay, int ammu)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        ArgumentNullException.ThrowIfNull(CombatState);
        
        if (this.CanonicalKeywords.Contains(TashkentKeyword.Barrage))
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(ammu * DynamicVars.Repeat.IntValue)
                .FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            UpdateAmmuGlobal(0);
        }
        else
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(DynamicVars.Repeat.IntValue)
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