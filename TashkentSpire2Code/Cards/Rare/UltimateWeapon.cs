using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Cards.Status;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class UltimateWeapon() : AmmunitionCard(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Fatal)];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AmmunitionDynamicVar(6M),
        new LoadDynamicVar(4M),
        new AmmuMaxDynamicVar(99M),
        new CalculationBaseVar(0M),
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

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        ArgumentNullException.ThrowIfNull(CombatState);

        int shellsToFire = await GetShellCountcmd.Execute(choiceContext, Owner, (int)CurrentAmmu, this.Keywords.Contains(TashkentKeyword.Barrage));
    
        if (shellsToFire > 0)
        {
            int overdrawnCount = 0;

            NHyperbeamVfx? nHyperbeamVfx = NHyperbeamVfx.Create(base.Owner.Creature, cardPlay.Target);
            if (nHyperbeamVfx != null)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nHyperbeamVfx);
                await Cmd.Wait(0.5f);
            }
            
            for (int i = 0; i < shellsToFire; i++) 
            {
                bool shouldTriggerFatal = cardPlay.Target.Powers.All((PowerModel p) => p.ShouldOwnerDeathTriggerFatal());
                AttackCommand attackCommand = await DamageCmd.Attack(base.DynamicVars.CalculatedDamage)
                    .FromCard(this, cardPlay)
                    .Targeting(cardPlay.Target)
                    .Execute(choiceContext);

                if (shouldTriggerFatal && attackCommand.Results.SelectMany((List<DamageResult> r) => r).Any((DamageResult r) => r.WasTargetKilled))
                {
                    int loadValue = DynamicVars["TashkentSpire2-Load"].IntValue;
                    await Loadcmd.Execute(choiceContext, this, loadValue);
                }

                if (CurrentAmmu > 0)
                {
                    UpdateAmmuGlobal(CurrentAmmu - 1);
                }
                else
                {
                    overdrawnCount++;
                }
            }

            if (overdrawnCount > 0 && this.Keywords.Contains(TashkentKeyword.Barrage))
            {
                List<CardModel> shellCasings = new List<CardModel>();
                for (int i = 0; i < overdrawnCount; i++)
                {
                    shellCasings.Add(base.CombatState.CreateCard<ShellCasing>(base.Owner));
                }
                await CardPileCmd.AddGeneratedCardsToCombat(shellCasings, PileType.Hand, base.Owner);
            }
        }
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}