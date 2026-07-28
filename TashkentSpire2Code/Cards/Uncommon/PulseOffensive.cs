using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Cards.Status;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class PulseOffensive() : AmmunitionCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6M, ValueProp.Move),
        new AmmunitionDynamicVar(2M),
        new LoadDynamicVar(1M),
        new AmmuMaxDynamicVar(6M),
        new PowerVar<WeakPower>(2M),
        new PowerVar<VulnerablePower>(2M),
        new ShotDynamicVar(3M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        
        int shellsLoaded = await GetShellCountcmd.Execute(choiceContext, Owner, (int)CurrentAmmu,this.Keywords.Contains(TashkentKeyword.Barrage));
        if (shellsLoaded > 0)
        {
            await TryTriggerShotEffectAsync(shellsLoaded, async () => {
                foreach (Creature enemy in base.CombatState.HittableEnemies)
                {
                    await PowerCmd.Apply<WeakPower>(choiceContext, enemy, base.DynamicVars.Weak.BaseValue, base.Owner.Creature, this);
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, base.DynamicVars.Vulnerable.BaseValue, base.Owner.Creature, this);
                }
            });
            
            for (int i = 0; i < shellsLoaded; i++)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this, cardPlay)
                    .TargetingAllOpponents(CombatState)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
            }
            int num = Math.Max(shellsLoaded - CurrentAmmu, 0);
            if (num > 0 && this.Keywords.Contains(TashkentKeyword.Barrage))
            {
                List<CardModel> list = new List<CardModel>();
                for (int i = 0; i < num; i++)
                {
                    list.Add(base.CombatState.CreateCard<ShellCasing>(base.Owner));
                }
                await CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Hand, base.Owner);
            }
            
            UpdateAmmuGlobal(Math.Max(CurrentAmmu - shellsLoaded, 0));
            await LoadAfterShotAsync(choiceContext, shellsLoaded);
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == this.Owner && cardPlay.Card.Keywords.Contains(CardKeyword.Exhaust))
        {
            int load = DynamicVars["TashkentSpire2-Load"].IntValue;
            await Loadcmd.Execute(context, this, load);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2M);
}