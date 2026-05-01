using Godot;
using MegaCrit.Sts2.Core.Combat;
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

public sealed class DistancePower : TashkentPower
{
    private const string VarKey = "Tashkent_Distance";
    private bool _isSyncing = false;
    private bool _isFlipping = false;
    
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;
    
    private int CurrentDist => (int)base.DynamicVars[VarKey].BaseValue;
    public override int DisplayAmount => CurrentDist;
    
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
    
    private int MapToDist(int amount) => amount - 10;
    
    public async Task OnDirectionFlipped()
    {
        if (_isSyncing) return;
        
        _isSyncing = true;
        _isFlipping = true;

        decimal targetAmount = 20m - base.Amount;
        decimal delta = targetAmount - base.Amount;
        
        if (delta != 0)
        {
            await PowerCmd.ModifyAmount(this, delta, base.Owner, null);
        }

        _isFlipping = false;
        _isSyncing = false;
    }
    
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? giver, out decimal modifiedAmount)
    {
        if (canonicalPower.Id == this.Id && target == this.Owner)
        {
            int potential = (int)base.Amount + (int)amount;
            int clamped = Mathf.Clamp(potential, 5, 15);
            modifiedAmount = (decimal)(clamped - (int)base.Amount);
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
        if (base.Amount < 5m)
        {
            await PowerCmd.ModifyAmount(this, 10m, base.Owner, cardSource);
            return;
        }
        
        int targetDist = MapToDist(base.Amount);
        base.DynamicVars[VarKey].BaseValue = targetDist;
        RefreshDerivedVars();

        if (targetDist != 0)
        {
            await UpdateCreaturePositions(targetDist);
        }
        InvokeDisplayAmountChanged();
    }
    
    private bool HasActiveSandpit()
    {
        if (base.CombatState == null) return false;
        return base.CombatState.Enemies.Any(e => e.HasPower<SandpitPower>());
    }
    
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == CombatSide.Enemy && HasActiveSandpit())
        {
            _isSyncing = true;  // 加锁

            await PowerCmd.ModifyAmount(this, 1m, base.Owner, null);
            
            _isSyncing = false;
        }
    }
    
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer == this.Owner && (!props.HasFlag(ValueProp.Move) || cardSource == null))
            return 1m;

        int dist = CurrentDist; 
        if (dist == 0) return 1m;

        int enemySide = 1;
        Creature? enemy = (dealer == base.Owner) ? target : dealer;
        if (enemy == null) return 1m;

        if (enemy.HasPower<BackAttackLeftPower>()) enemySide = -1;

        var surrounded = base.Owner.GetPower<SurroundedPower>();
        int playerFacing = (surrounded != null && surrounded.Facing == SurroundedPower.Direction.Left) ? -1 : 1;

        bool isFrontEnemy = (enemySide == playerFacing);

        decimal multiplier = 1m;
        decimal weight = isFrontEnemy ? (decimal)dist : -(decimal)dist;

        if (dealer == base.Owner)
            multiplier *= (1m + weight * 0.2m);
        else if (target == base.Owner)
            multiplier *= (1m + weight * 0.1m);

        return multiplier;
    }
    
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal oldAmount, Creature? __, CardModel? cardSource)
    {
        if (power == this)
        {
            int newDist = MapToDist(base.Amount);
            int lastLogicalDist = (int)base.DynamicVars[VarKey].BaseValue;
            int deltaDist = newDist - lastLogicalDist;

            if (deltaDist != 0)
            {
                base.DynamicVars[VarKey].BaseValue = newDist;
                RefreshDerivedVars();
                
                if (!_isSyncing)
                {
                    _isSyncing = true;
                    await SyncSandpitPower(deltaDist);
                    _isSyncing = false;
                }

                if (!_isFlipping)
                {
                    await UpdateCreaturePositions(deltaDist);
                }
                await NotifyDistanceChanged(deltaDist);
                InvokeDisplayAmountChanged();
            }
        }
    }
    
    private async Task NotifyDistanceChanged(int delta)
    {
        if (delta == 0) return;

        var powers = Owner.Powers
            .OfType<DefendTerritorialWatersPower>();

        foreach (var p in powers)
        {
            await p.OnDistanceChanged(delta);
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
                await PowerCmd.ModifyAmount(sandpitPower, -(decimal)delta, enemy, null);
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
        var surrounded = base.Owner.GetPower<SurroundedPower>();
        bool isFacingLeft = surrounded != null && surrounded.Facing == SurroundedPower.Direction.Left;

        float moveDir = isFacingLeft ? -1f : 1f;
        float moveDistance = delta * 50f * moveDir;

        Tween? tween = null;
        foreach (Creature creature in GetOwnerAndPets())
        {
            NCreature? node = NCombatRoom.Instance?.GetCreatureNode(creature);
            if (node == null || creature.IsDead) continue;
            if (tween == null) tween = NCombatRoom.Instance?.CreateTween().SetParallel().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            tween?.TweenProperty(node, "global_position:x", node.GlobalPosition.X + moveDistance, 0.25f);
        }
        if (tween != null) await tween.ToSignal(tween, Tween.SignalName.Finished);
    }
    
    public async Task ModifyAmountFromEscape(decimal delta, CardModel cardSource)
    {
        if (_isSyncing) return;
    
        _isSyncing = true;
        try 
        {
            await PowerCmd.ModifyAmount(this, delta, base.Owner, cardSource);
        }
        finally 
        {
            _isSyncing = false;
        }
    }
}