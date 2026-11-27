using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class ArmorBreakPower : IndomitablePower, IBeforeDamageResultsSubscriber
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 使用橙金色作为破甲层数的高亮颜色
    public override Color AmountLabelColor => new("909090"); 
    
    internal int GetBlockLoss(PendingHit hit) =>
        hit.Target == Owner && hit.Damage > 0 && hit.Props.IsPoweredAttack()
            ? Math.Min(Owner.Block, Amount) : 0;
    
    // 基于 CreatureCmd.Damage 的预运算机制的变更，BeforeDamageReceived 钩子不再适合，改用自定义的预运算之前的钩子。
    public async Task BeforeDamageResults(PlayerChoiceContext choiceContext, PendingHit hit)
    {
        var loss = GetBlockLoss(hit);
        if (loss <= 0) return;
        Flash();
        await CreatureCmd.LoseBlock(choiceContext, Owner, loss, hit.Dealer);
    }
    
    // 机制补充：在拥有者的回合结束时，层数减少 1
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy) await PowerCmd.TickDownDuration(this);
    }
}