using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public class DistancePower : TashkentPower
{
    private const string VarKey = "Tashkent_Distance";
    
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/distance_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/distance_power.png";
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        new List<DynamicVar>
        {
            new DynamicVar(VarKey, 0M),
            new DynamicVar("Increase", 0M),
            new DynamicVar("Decrease", 0M)
        };
    
    private int CurrentDist => (int)base.DynamicVars[VarKey].BaseValue;
    
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? giver, out decimal modifiedAmount)
    {
        if (canonicalPower.Id == this.Id) {
            int potential = CurrentDist + (int)amount;
            int clamped = Mathf.Clamp(potential, -5, 5);
            
            modifiedAmount = clamped - CurrentDist;
            return true;
        }
        modifiedAmount = amount;
        return false;
    }
    
    private void RefreshDerivedVars()
    {
        int dist = CurrentDist;

        base.DynamicVars["Increase"].BaseValue = dist * 20;
        base.DynamicVars["Decrease"].BaseValue = dist * 10;
    }
    
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        int initialAmount = Mathf.Clamp(base.Amount, -5, 5);
        base.DynamicVars[VarKey].BaseValue = initialAmount;
        RefreshDerivedVars();
        
        await UpdateCreaturePositions(initialAmount);
        InvokeDisplayAmountChanged();
    }
    
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer == this.Owner && (!props.HasFlag(ValueProp.Move) || cardSource == null))
            return 1m;
        
        decimal multiplier = 1m;
        
        if (dealer == base.Owner)
            multiplier *= (1m + (decimal)CurrentDist * 0.2m);
        if (target == base.Owner)
            multiplier *= (1m + (decimal)CurrentDist * 0.1m);

        return multiplier;
    }
    
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal oldAmount, Creature? __, CardModel? cardSource)
    {
        if (power == this) {
            int newAmount = Mathf.Clamp(base.Amount, -5, 5);
            int delta = newAmount - CurrentDist;

            if (delta != 0) {
                base.DynamicVars[VarKey].BaseValue = newAmount;
                RefreshDerivedVars();
                
                await UpdateCreaturePositions(delta);
                
                await SyncSandpitPower(delta);  //契合沙虫机制
                
                InvokeDisplayAmountChanged();
            }
        }
    }
    
    private async Task SyncSandpitPower(int delta)
    {
        var sandpitEnemies = base.CombatState?.Enemies.Where(c => c.HasPower<SandpitPower>());
    
        if (sandpitEnemies == null) return;

        foreach (var enemy in sandpitEnemies)
        {
            var sandpitPower = enemy.Powers.OfType<SandpitPower>()
                .FirstOrDefault(s => s.Target == base.Owner);

            if (sandpitPower != null)
            {
                await PowerCmd.ModifyAmount(sandpitPower, (decimal)delta, enemy, null);
            }
        }
    }
    
    private List<Creature> GetOwnerAndPets()
    {
        List<Creature> result = new List<Creature>();
        
        result.Add(base.Owner);

        if (base.Owner.Pets != null)
            foreach (var pet in base.Owner.Pets)
                result.Add(pet);

        return result;
    }
    
    private async Task UpdateCreaturePositions(int delta)
    {
        if (delta == 0) return;

        float moveDistance = delta * 50f;

        Tween? tween = null;

        foreach (Creature creature in GetOwnerAndPets())
        {
            if (creature.IsDead) continue;

            NCreature? node = NCombatRoom.Instance?.GetCreatureNode(creature);
            if (node == null) continue;

            if (tween == null) {
                tween = NCombatRoom.Instance?.CreateTween()
                    .SetParallel()
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);
            }

            tween?.TweenProperty(
                node,
                "global_position:x",
                node.GlobalPosition.X + moveDistance,
                0.25f
            );
        }

        if (tween != null)
            await tween.ToSignal(tween, Tween.SignalName.Finished);
        
    }
}