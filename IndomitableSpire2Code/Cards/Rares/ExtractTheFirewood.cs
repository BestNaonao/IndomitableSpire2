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
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class ExtractTheFirewood() : IndomitableCard(3, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(10),
        new DamageVar(5M, ValueProp.Move),
        new CustomPowerVar<OnFirePower>(3M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;
        
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue);
        var selectedCards = (await CardSelectCmd.FromCombatPile(
            choiceContext, PileType.Discard.GetPile(Owner), Owner, prefs
        )).ToList();
        
        foreach (var card in selectedCards.TakeWhile(_ => !CombatManager.Instance.IsOverOrEnding))
        {
            await CardCmd.Exhaust(choiceContext, card);
            if (CombatManager.Instance.IsOverOrEnding) break;
            
            // 每张牌重新选择目标；本次伤害与起火作用于同一个敌人。
            var enemy = Owner.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies);
            if (enemy == null) continue;
            
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(enemy)
                .WithHitFx("vfx/vfx_fire_burst")
                .Execute(choiceContext);
            if (enemy.IsAlive)
                await PowerCmd.Apply<OnFirePower>(
                    choiceContext, 
                    enemy, 
                    DynamicVars.OnFire().BaseValue, 
                    Owner.Creature, 
                    this);
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2M);
        DynamicVars.OnFire().UpgradeValueBy(1M);
        AddKeyword(CardKeyword.Retain);
    }
}
