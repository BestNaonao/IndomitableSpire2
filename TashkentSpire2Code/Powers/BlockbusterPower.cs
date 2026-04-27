using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Powers;

public sealed class BlockbusterPower : TashkentPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override string CustomBigIconPath => 
        "res://TashkentSpire2/images/powers/big/mark_power.png";
    public override string CustomPackedIconPath => 
        "res://TashkentSpire2/images/powers/packed/mark_power.png";
    
    public override async Task AfterAttack(AttackCommand command)
    {
        if (command.Attacker != base.Owner || command.TargetSide == base.Owner.Side || !command.DamageProps.IsPoweredAttack())
        {
            return;
        }

        int attackCount = CombatManager.Instance.History.CardPlaysStarted.Count(e => 
            e.HappenedThisTurn(base.CombatState) && 
            e.CardPlay.Card.Type == CardType.Attack && 
            e.CardPlay.Card.Owner.Creature == base.Owner);

        if (attackCount > 1)
        {
            return;
        }
        
        decimal totalDamage = command.Results.Sum(r => r.TotalDamage + r.OverkillDamage) * this.Amount / 100M;

        if (totalDamage > 0)
        {
            await CreatureCmd.GainBlock(base.Owner, totalDamage, ValueProp.Move, null);
        }
    }
}