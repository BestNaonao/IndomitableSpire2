using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Rares;

public sealed class HappyBirthday() : IndomitableCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const string OnFireThreshold = "OnFireThreshold";
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(2M, ValueProp.Move),
        new CustomPowerVar<OnFirePower>(2M),
        new CustomPowerVar<OnFirePower>(OnFireThreshold, 8M)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        var target = cardPlay.Target;
        if (CombatState == null) return;
        // 将所有段伤害归为同一次攻击，统一触发攻击前后的能力与遗物钩子。
        await using var attackContext = await AttackCommand.CreateContextAsync(CombatState, choiceContext, cardPlay);
        while (CombatManager.Instance.IsInProgress && target.IsHittable && CombatState.ContainsCreature(target))
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "Attack", Owner.Character.AttackAnimDelay);
            var results = await CreatureCmd.Damage(
                choiceContext, target, DynamicVars.Damage, Owner.Creature, this, cardPlay);
            attackContext.AddHit(results);
            if (!CombatManager.Instance.IsInProgress || !target.IsHittable || !CombatState.ContainsCreature(target))
                break;
            await PowerCmd.Apply<OnFirePower>(
                choiceContext: choiceContext, 
                target: target, 
                amount: DynamicVars.OnFire().BaseValue, 
                applier: Owner.Creature, 
                cardSource: this
                );
            // 每次完整结算伤害与起火后检查阈值；多人模式下合计所有施加者的起火。
            if (target.Powers.OfType<OnFirePower>().Sum(power => power.Amount) >= DynamicVars[OnFireThreshold].BaseValue)
                break;
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars[OnFireThreshold].UpgradeValueBy(4M);
    }
}