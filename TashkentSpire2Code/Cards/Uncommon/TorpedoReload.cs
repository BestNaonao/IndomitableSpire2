using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards.Status;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Uncommon;

public sealed class TorpedoReload() : AmmunitionCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(18M),
        new AmmunitionDynamicVar(0M),
        new LoadDynamicVar(1M),
        new AmmuMaxDynamicVar(6M),
        new ShotDynamicVar(2M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        
        int shellsLoaded = await GetShellCountcmd.Execute(choiceContext, Owner, (int)CurrentAmmu,this.Keywords.Contains(TashkentKeyword.Barrage));
        if (shellsLoaded > 0)
        {
            for (int i = 0; i < shellsLoaded; i++)
            {
                int turns = TorpedoPower.ComputeTurns(base.Owner.Creature);
                (await PowerCmd.Apply<TorpedoPower>(base.Owner.Creature, (decimal)turns, base.Owner.Creature, this))
                    ?.SetDamage(DynamicVars["TashkentSpire2-Torpedo"].BaseValue);
            }
            if (shellsLoaded >= DynamicVars["TashkentSpire2-Shot"].BaseValue)
            {
                var powers = base.Owner.Creature.Powers
                    .Where(p => p is TorpedoPower)
                    .ToList();

                foreach (var power in powers)
                {
                    int current = (int)power.Amount;
                    int target = Math.Max(1, current - 1);
                    int delta = target - current;

                    if (delta != 0)
                    {
                        await PowerCmd.ModifyAmount(power, delta, base.Owner.Creature, this);
                    }
                }
            }
            
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
    }
    
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        
        int load = DynamicVars["TashkentSpire2-Load"].IntValue;
        await Loadcmd.Execute(choiceContext, this, load);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["TashkentSpire2-Shot"].UpgradeValueBy(-1M);
    }
}