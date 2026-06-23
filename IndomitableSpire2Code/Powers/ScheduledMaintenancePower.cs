using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class ScheduledMaintenancePower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0) return;
        
        // 1. 【由发起者组装数据池】：扫描手牌和抽牌堆
        var damagedAircraft = CardPile.GetCards(player, PileType.Hand, PileType.Draw)
            .Where(c => c.IsCarrierAircraft() && !c.IsFullDurability()).ToList();
        
        // 如果有需要维修的，就闪烁，并将装配好的数据池喂给底层 Command
        if (damagedAircraft.Count > 0)
        {
            Flash();
            await RepairCmd.RandomRepair(player, damagedAircraft, Amount);
        }
    }
}