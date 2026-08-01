using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Cards.Status;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public sealed class FocusFire() : AmmunitionCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AmmunitionDynamicVar(6M),
        new LoadDynamicVar(6M),
        new AmmuMaxDynamicVar(6M),
        new ShotDynamicVar(6M),
        new CalculationBaseVar(1M),
        new ExtraDamageVar(1M),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) =>
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
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        ArgumentNullException.ThrowIfNull(CombatState);
        
        int shellsLoaded = await GetShellCountcmd.Execute(choiceContext, Owner, (int)CurrentAmmu,this.Keywords.Contains(TashkentKeyword.Barrage));
        if (shellsLoaded > 0)
        {
            await TryTriggerShotEffectAsync(shellsLoaded, async () => {
                await DamageCmd.Attack(base.DynamicVars.CalculatedDamage).FromCard(this, cardPlay).Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
            });
            
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
        
        int load = DynamicVars["TashkentSpire2-Load"].IntValue;
        await Loadcmd.Execute(choiceContext, this, load);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3M);
    }
}