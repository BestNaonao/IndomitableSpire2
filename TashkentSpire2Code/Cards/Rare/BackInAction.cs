using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class BackInAction() : TashkentCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        
        var candidates = PileType.Discard.GetPile(base.Owner).Cards
            .Where(c => !c.Keywords.Contains(CardKeyword.Unplayable))
            .ToList();
        
        if (candidates.Count == 0)
        {
            return;
        }
        
        if (candidates.Count == 1)
        {
            if (CombatManager.Instance.IsOverOrEnding)
            {
                return;
            }

            var card = candidates[0];

            Creature? target = null;
            if (card.TargetType == TargetType.AnyEnemy)
            {
                var enemies = CombatState.HittableEnemies;
                if (enemies.Any())
                {
                    target = Owner.RunState.Rng.CombatTargets.NextItem(enemies);
                }
            }

            await CardCmd.AutoPlay(choiceContext, card, target);
            return;
        }
        
        var selectedList = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            base.Owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 2){RequireManualConfirmation = true} 
        );
        
        foreach (var card in selectedList)
        {
            if (CombatManager.Instance.IsOverOrEnding)
            {
                break;
            }
            
            Creature? target = null;
            if (card.TargetType == TargetType.AnyEnemy)
            {
                var enemies = base.CombatState.HittableEnemies;
                if (enemies != null && enemies.Any())
                {
                    target = base.Owner.RunState.Rng.CombatTargets.NextItem(enemies);
                }
            }
            
            await CardCmd.AutoPlay(choiceContext, card, target);
        }
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}