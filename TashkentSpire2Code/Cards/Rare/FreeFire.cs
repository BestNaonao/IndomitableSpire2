using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards.Status;
using TashkentSpire2.TashkentSpire2Code.Commands;
using TashkentSpire2.TashkentSpire2Code.Keywords;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class FreeFire() : AmmunitionCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [TashkentKeyword.Barrage];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AmmunitionDynamicVar(0M),
        new LoadDynamicVar(0M),
        new AmmuMaxDynamicVar(6M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
    
        int shellsLoaded = await GetShellCountcmd.Execute(choiceContext, Owner, (int)CurrentAmmu, this.Keywords.Contains(TashkentKeyword.Barrage));
        if (shellsLoaded > 0)
        {
            var ammuCards = ModelDb.AllCards.OfType<AmmunitionCard>();
        
            List<CardModel> cardsToPlay = CardFactory.GetDistinctForCombat(
                base.Owner, ammuCards, shellsLoaded, base.Owner.RunState.Rng.CombatCardGeneration
            ).ToList();

            foreach (var card in cardsToPlay)
            {
                if (this.IsUpgraded) 
                {
                    CardCmd.Upgrade(card);
                }
            }

            if (cardsToPlay.Count > 0)
            {
                await CardPileCmd.AddGeneratedCardsToCombat(cardsToPlay, PileType.Play, base.Owner);
            }

            foreach (CardModel item in cardsToPlay)
            {
                if (!base.Owner.Creature.IsDead)
                {
                    item.ExhaustOnNextPlay = true; 
                    await CardCmd.AutoPlay(choiceContext, item, null);
                }
                else
                {
                    break;
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
                await CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Hand, base.Owner);
            }
            
            UpdateAmmuGlobal(Math.Max(CurrentAmmu - shellsLoaded, 0));
            await LoadAfterShotAsync(choiceContext, shellsLoaded);
        }
    }
}