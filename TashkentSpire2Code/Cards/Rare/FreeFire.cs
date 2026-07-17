using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Cards.Status;
using TashkentSpire2.TashkentSpire2Code.Cards.Token;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class FreeFire() : AmmunitionCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [TashkentKeyword.Barrage];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5M, ValueProp.Move),
        new AmmunitionDynamicVar(0M),
        new LoadDynamicVar(0M),
        new AmmuMaxDynamicVar(6M),
        new ShotDynamicVar(1M),
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Pursuit>()];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        ArgumentNullException.ThrowIfNull(CombatState);
        
        int shellsLoaded = await GetShellCountcmd.Execute(choiceContext, Owner, (int)CurrentAmmu,this.Keywords.Contains(TashkentKeyword.Barrage));
        if (shellsLoaded > 0)
        {
            await TryTriggerShotEffectAsync(shellsLoaded, async () => {
                await Pursuit.CreateInHand(base.Owner, base.DynamicVars.Cards.IntValue, base.CombatState);
            });
            
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .WithHitCount(shellsLoaded)
                .Targeting(cardPlay.Target)
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
                await CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Hand, base.Owner);
            }
            
            UpdateAmmuGlobal(Math.Max(CurrentAmmu - shellsLoaded, 0));
            await LoadAfterShotAsync(choiceContext, shellsLoaded);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2M);
    }
}