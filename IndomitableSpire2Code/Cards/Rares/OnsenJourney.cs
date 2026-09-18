using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class OnsenJourney() : IndomitableCard(2, CardType.Skill, CardRarity.Rare, TargetType.RandomEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(10),
        new CustomPowerVar<FloodingPower>(3M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;
        
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue);
        var selectedCards = (await CardSelectCmd.FromCombatPile(
            choiceContext, PileType.Draw.GetPile(Owner), Owner, prefs
        )).ToList();
        
        foreach (var card in selectedCards.TakeWhile(_ => !CombatManager.Instance.IsOverOrEnding))
        {
            var result = await CardPileCmd.Add(card, PileType.Hand);
            // 满手时 Add 仍可能返回 success=true，但牌会进入弃牌堆；按实际入手结果触发。
            if (!result.success || result.cardAdded.Pile?.Type != PileType.Hand) continue;
            if (CombatManager.Instance.IsOverOrEnding) break;
            
            var enemy = Owner.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies);
            if (enemy == null) continue;
            
            await PowerCmd.Apply<FloodingPower>(
                choiceContext, enemy, DynamicVars.Flooding().BaseValue, Owner.Creature, this);
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Flooding().UpgradeValueBy(1M);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
