using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class LastGaspPower : IndomitablePower, IDurabilityLossModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
        => dealer == Owner && cardSource != null && cardSource.Owner.Creature == Owner &&
            props.IsPoweredAttack() && cardSource.IsCarrierAircraft() ? 2M : 1M;
    
    // 与机动规避共用损失修正钩子：乘 2 和归零的先后顺序不影响最终的 0 点损失。
    public bool TryModifyDurabilityLoss(CardModel card, int originalLoss, out int modifiedLoss)
    {
        modifiedLoss = originalLoss;
        if (card.Owner.Creature != Owner || !card.IsCarrierAircraft() || originalLoss <= 0) return false;
        modifiedLoss = (int)Math.Min((long)originalLoss * 2, int.MaxValue);
        return true;
    }
    
    public Task AfterModifyingDurabilityLoss(CardModel card)
    {
        Flash();
        return Task.CompletedTask;
    }
    
    // Amount 仅表示剩余回合数，伤害和损失的倍率始终为 2。
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side && participants.Contains(Owner)) await PowerCmd.Decrement(this);
    }
}