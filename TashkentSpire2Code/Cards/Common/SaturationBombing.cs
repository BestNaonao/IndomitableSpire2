using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Cards.Status;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Common;

public sealed class SaturationBombing() : AmmunitionCard(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5M, ValueProp.Move),
        new AmmunitionDynamicVar(3M),
        new AmmuMaxDynamicVar(6M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        
        await SaturationBombingcmd.Execute(choiceContext, this.Owner, this);
        
        int shellsLoaded = await GetShellCountcmd.Execute(choiceContext, Owner, (int)CurrentAmmu,this.Keywords.Contains(TashkentKeyword.Barrage));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(shellsLoaded)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        int num = Math.Max(shellsLoaded - CurrentAmmu, 0);
        if (num > 0 && this.Keywords.Contains(TashkentKeyword.Barrage))
        {
            List<CardModel> list = new List<CardModel>();
            for (int i = 0; i < num; i++)
            {
                list.Add(base.CombatState.CreateCard<ShellCasing>(base.Owner));
            }
            await CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Hand, addedByPlayer: true);
        }
        
        UpdateAmmuGlobal(Math.Max(CurrentAmmu - shellsLoaded, 0));
    }
    
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3M);
}