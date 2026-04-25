using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Commands;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public sealed class FocusFire() : AmmunitionCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
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
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(4M, ValueProp.Move),
        new AmmunitionDynamicVar(1M),
        new LoadDynamicVar(1M),
        new AmmuMaxDynamicVar(1M),
        new CalculationBaseVar(0M),
        new CalculationExtraVar(1M),
        new CalculatedVar("TashkentHits")
            .WithMultiplier((CardModel card, Creature? _) =>
            {
                return card.Owner?.PlayerCombatState?.AllCards?.Sum(c =>
                {
                    if (c.DynamicVars != null &&
                        c.DynamicVars.TryGetValue("TashkentSpire2-Ammu", out var ammuVar))
                        return ammuVar.IntValue;
                    return 0;
                }) ?? 0;
            })
    ];
    
    protected override async Task OnPlayWithAmmu(PlayerChoiceContext choiceContext, CardPlay cardPlay, int ammu)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        ArgumentNullException.ThrowIfNull(CombatState);
        
        int hits = (int)((CalculatedVar)DynamicVars["TashkentHits"])
            .Calculate(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits).FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        UpdateAmmuGlobal(ammu - 1);
    }

    protected override async Task OnPlayWithoutAmmu(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int load = DynamicVars["TashkentSpire2-Load"].IntValue;
        await Loadcmd.Execute(choiceContext, this, load);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1M);
    }
}