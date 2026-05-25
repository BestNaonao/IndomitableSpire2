using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class EvasiveManeuversPower : IndomitablePower, IDurabilityLossModifier
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 阶段 1：修改数值（无论在预览还是实战中都会频繁触发）
    public bool TryModifyDurabilityLoss(CardModel card, int originalLoss, out int modifiedLoss)
    {
        modifiedLoss = originalLoss;
        if (card.Owner.Creature != Owner || !card.IsCarrierAircraft()) return false;
        modifiedLoss = 0;   // 伤害降为 0
        return true;    // 报告我生效了
    }
    
    // 阶段 2：后置结算（仅在实战结算完毕后才会触发）
    public async Task AfterModifyingDurabilityLoss(CardModel card)
    {
        if (card.Owner.Creature != Owner || !card.IsCarrierAircraft()) return;
        Flash();
        await PowerCmd.Decrement(this); // 此时扣减层数安全且逻辑完美
    }
    
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side)
            await PowerCmd.Remove(this);
    }
}