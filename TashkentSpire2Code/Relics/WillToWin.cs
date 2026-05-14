using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class WillToWin : TashkentRelic
{
    private bool _isActivating;
    private bool _usedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Rare;
    
    public override bool ShowCounter => DisplayAmount > -1;

    public override int DisplayAmount
    {
        get
        {
            if (!CombatManager.Instance.IsInProgress || base.IsCanonical)
            {
                return -1;
            }
            return UsedThisTurn ? 0 : 1;
        }
    }

    private bool IsActivating
    {
        get => _isActivating;
        set
        {
            AssertMutable();
            _isActivating = value;
            InvokeDisplayAmountChanged();
        }
    }

    private bool UsedThisTurn
    {
        get => _usedThisTurn;
        set
        {
            AssertMutable();
            _usedThisTurn = value;
            InvokeDisplayAmountChanged();
        }
    }

    protected override string BigIconPath => "res://TashkentSpire2/images/relics/big/EngineBoost.png";
    public override string PackedIconPath => "res://TashkentSpire2/images/relics/packed/EngineBoost.png";
    protected override string PackedIconOutlinePath => "res://TashkentSpire2/images/relics/outline/EngineBoost.png";

    public override bool ShouldDie(Creature creature)
    {
        if (creature != base.Owner.Creature)
        {
            return true;
        }

        if (!UsedThisTurn)
        {
            return false;
        }

        var vigor = creature.GetPower<VigorPower>();
        if (vigor != null && vigor.Amount >= 20m)
        {
            return false;
        }

        return true;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (!UsedThisTurn)
        {
            UsedThisTurn = true;
            await TriggerHealEffect();
            return;
        }

        var vigor = creature.GetPower<VigorPower>();
        if (vigor != null && vigor.Amount >= 20m)
        {
            await PowerCmd.Apply<VigorPower>(base.Owner.Creature, -20M, base.Owner.Creature, null);
            await TriggerHealEffect();
        }
    }
    
    private async Task TriggerHealEffect()
    {
        base.Status = RelicStatus.Active; 
        await DoActivateVisuals(); 

        await CreatureCmd.Heal(this.Owner.Creature, 1m);
        
        base.Status = RelicStatus.Normal;
    }

    private async Task DoActivateVisuals()
    {
        IsActivating = true;
        Flash();
        await Cmd.Wait(0.5f);
        IsActivating = false;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (!(room is CombatRoom))
        {
            return Task.CompletedTask;
        }
        UsedThisTurn = false;
        base.Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UsedThisTurn = true;
        base.Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }
    
    public override async Task AfterAttack(AttackCommand command)
    {
        if (command.Attacker != base.Owner.Creature || command.TargetSide == base.Owner.Creature.Side || !command.DamageProps.IsPoweredAttack() || base.Owner.Creature.CurrentHp != 1M)
        {
            return;
        }
        
        decimal totalDamage = command.Results.Sum(r => r.TotalDamage + r.OverkillDamage) * 20M / 100M;

        if (totalDamage > 0)
        {
            await PowerCmd.Apply<VigorPower>(base.Owner.Creature, totalDamage, base.Owner.Creature, null);
        }
    }
}