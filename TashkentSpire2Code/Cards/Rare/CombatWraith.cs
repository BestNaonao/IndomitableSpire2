using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
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

public sealed class CombatWraith() : AmmunitionCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(8M, ValueProp.Move),
        new EnergyVar(1),
        new AmmunitionDynamicVar(3M),
        new LoadDynamicVar(3M),
        new AmmuMaxDynamicVar(6M),
        new ShotDynamicVar(2M)
    ];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Vodka>(base.IsUpgraded)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        ArgumentNullException.ThrowIfNull(CombatState);
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        int shellsLoaded = await GetShellCountcmd.Execute(choiceContext, Owner, (int)CurrentAmmu,this.Keywords.Contains(TashkentKeyword.Barrage));
        if (shellsLoaded > 0)
        {
            if (shellsLoaded >= DynamicVars["TashkentSpire2-Shot"].BaseValue)
            {
                List<Vodka> list = Vodka.Create(base.Owner, base.DynamicVars.Cards.IntValue, base.CombatState).ToList();
        
                if (base.IsUpgraded)
                {
                    foreach (Vodka item in list)
                    {
                        CardCmd.Upgrade(item);
                    }
                }
        
                foreach (Vodka item in list)
                {
                    await CardPileCmd.AddGeneratedCardToCombat(item, PileType.Hand, addedByPlayer: true);
                }
            }
            
            await PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue * shellsLoaded, base.Owner);
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
    
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player == base.Owner && combatState.RoundNumber == 1)
        {
            int load = DynamicVars["TashkentSpire2-Load"].IntValue;
            await Loadcmd.Execute(choiceContext, this, load);
        }
    }
}