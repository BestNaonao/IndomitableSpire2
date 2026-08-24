using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

/// <summary>
/// 一次待执行/执行中的认真模式额外回合。Amount（Y）是经过所有层数修改器后的实际强度。
/// </summary>
public sealed class EarnestModePower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override object InitInternalData() => new EarnestModeState();
    
    public override bool ShouldTakeExtraTurn(Player player)
    {
        var state = GetInternalData<EarnestModeState>();
        return Amount > 0 && state.IsPending && player == Owner.Player;
    }
    
    /// <summary>
    /// 原版此钩子虽然叫 AfterTakingExtraTurn，实际是在确认额外回合后、额外回合开始前调用。
    /// </summary>
    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player == Owner.Player && GetInternalData<EarnestModeState>() is {IsPending : true} state)
        {
            state.IsPending = false;
            state.IsActive = true;
            Flash();
            EarnestModeButton.RequestRefresh();
        }
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// SetupPlayerTurn 已完成基础能量重置和基础抽牌后，再追加 Y 点能量与 Y 张牌。
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsThisEarnestModeTurn(player)) return;
        Flash();
        var bonusAmount = Amount;
        await PlayerCmd.GainEnergy(bonusAmount, player);
        await CardPileCmd.Draw(choiceContext, bonusAmount, player);
    }
    
    /// <summary>
    /// 认真模式额外回合内，自己的每张舰载机牌额外结算 Y 次。
    /// </summary>
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        Amount <= 0 || card.Owner != Owner.Player || !card.IsCarrierAircraft() || !IsThisEarnestModeTurn(Owner.Player) 
            ? playCount : playCount + Amount;
    
    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        Flash();
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 在认真模式额外回合的所有回合结束效果之后清理自身与干劲。
    /// </summary>
    public override async Task AfterSideTurnEndLate(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner.Player is null ||side != CombatSide.Player || !participants.Contains(Owner) || 
            !CombatManager.Instance.PlayersTakingExtraTurn.Contains(Owner.Player) || 
            !GetInternalData<EarnestModeState>().IsActive)
            return;
        // 重置干劲能力，重置全神贯注能力
        Owner.GetPower<MotivationPower>()?.Restart();
        Owner.GetPower<FullConcentrationPower>()?.NotifyEarnestModeEnded();
        await PowerCmd.Remove(this);
    }
    
    public override Task AfterRemoved(Creature oldOwner)
    {
        EarnestModeButton.RequestRefresh();
        return Task.CompletedTask;
    }
    
    private bool IsThisEarnestModeTurn(Player player) => 
        GetInternalData<EarnestModeState>().IsActive &&
        CombatManager.Instance.IsInProgress && CombatManager.Instance.PlayersTakingExtraTurn.Contains(player);
    
    private sealed class EarnestModeState
    {
        public bool IsPending = true;   // 本质上是与 IsActive 相反的标志位，表示认真模式被赋予但是还未进入额外回合
        public bool IsActive;   // 表示认真模式被激活
    }
}