using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class TargetShipPower : TashkentPower
{
    private const string StoredDamageKey = "StoredDamage";

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    
    public override int DisplayAmount => (int)base.DynamicVars[StoredDamageKey].BaseValue;

    public override string CustomBigIconPath =>
        "res://TashkentSpire2/images/powers/big/targetship_power.png";

    public override string CustomPackedIconPath =>
        "res://TashkentSpire2/images/powers/packed/targetship_power.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar(StoredDamageKey, 0m)
    ];
    
    public override Task AfterCurrentHpChanged(Creature target, decimal hpChange)
    {
        if (target == base.Owner && hpChange < 0)
        {
            base.DynamicVars[StoredDamageKey].BaseValue += Math.Abs(hpChange);
            InvokeDisplayAmountChanged();
            
            Flash();
        }
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(base.Owner))
        {
            return;
        }

        var owner = base.Owner;
        if (owner == null || combatState == null)
            return;

        int damageToDeal = (int)base.DynamicVars[StoredDamageKey].BaseValue * this.Amount;

        if (damageToDeal <= 0)
            return;

        var teammates = combatState.GetTeammatesOf(owner)
            .Where(c => c.IsAlive)
            .ToList();

        var ctx = new HookPlayerChoiceContext(this, LocalContext.NetId!.Value, combatState, GameActionType.Combat);

        var dmg = new DamageVar(damageToDeal, ValueProp.Unpowered);
        await CreatureCmd.Damage(ctx, CombatState.HittableEnemies, dmg, Owner);

        base.DynamicVars[StoredDamageKey].BaseValue = 0m;
        InvokeDisplayAmountChanged();
    }
    
    public override async Task BeforeDeath(Creature target)
    {
        if (base.Owner == target)
        {
            int damageToDeal = (int)base.DynamicVars[StoredDamageKey].BaseValue * this.Amount;

            if (damageToDeal <= 0)
                return;

            var combatState = base.CombatState;
            if (combatState == null)
                return;

            var aliveMonsterTeammates = combatState.Enemies
                .Where(c => c != null && c != base.Owner && c.CurrentHp > 0)
                .ToList();

            if (aliveMonsterTeammates.Count == 0)
                return;

            var dmg = new DamageVar(damageToDeal, ValueProp.Unpowered);

            var ctx = new HookPlayerChoiceContext(this, LocalContext.NetId!.Value, combatState, GameActionType.Combat);

            await CreatureCmd.Damage(ctx, aliveMonsterTeammates, dmg, base.Owner);
        }
    }
}