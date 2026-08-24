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
/// “全神贯注”的逻辑中转能力。Amount（X）决定点击按钮时尝试给予的认真模式层数。
/// </summary>
public sealed class FullConcentrationPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override object InitInternalData() => new ConcentrationState();
    
    /// <summary>
    /// 满干劲、尚未提交过本轮认真模式、且没有待执行/执行中的认真模式时才可激活。
    /// </summary>
    public bool IsActivationAvailable =>
        Amount > 0 && !GetInternalData<ConcentrationState>().ActivationCommitted &&
        Owner.GetPower<MotivationPower>() is { IsCompleted: true } &&
        Owner.GetPower<EarnestModePower>() is null;
    
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncMotivationState(flashWhenReady: false);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 监听干劲的每次层数变化，并据此刷新按钮的显示/使能状态。
    /// </summary>
    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is MotivationPower && power.Owner == Owner)
            SyncMotivationState(flashWhenReady: true);
        else if (power == this)
            EarnestModeButton.RequestRefresh();
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 本地按钮使用的最终放行条件。这里可以读取当前客户端的战斗输入状态。
    /// </summary>
    public bool CanEnterEarnestMode(Player player) => 
        player == Owner.Player && IsActivationAvailable && 
        CombatManager.Instance.IsInProgress && !CombatManager.Instance.PlayerActionsDisabled && 
        !CombatManager.Instance.IsPlayerReadyToEndTurn(player) && 
        CombatManager.Instance.IsPartOfPlayerTurn(player) && 
        player.PlayerCombatState?.Phase == PlayerTurnPhase.Play;
    
    /// <summary>
    /// 同步 Action 的确定性校验。不能读取 PlayerActionsDisabled、当前屏幕等本地 UI 状态，
    /// 因为远端客户端执行别人的 Action 时这些值可以不同，否则会导致一端应用能力、另一端跳过应用。
    /// </summary>
    public bool CanApplyEarnestMode(Player player) => player == Owner.Player && IsActivationAvailable;
    
    /// <summary>
    /// 由 EarnestModeButtonAction 在所有客户端上同步执行。
    /// </summary>
    public async Task EnterEarnestMode(PlayerChoiceContext choiceContext, Player player)
    {
        if (!CanApplyEarnestMode(player))
        {
            EarnestModeButton.RequestRefresh();
            return;
        }
        var state = GetInternalData<ConcentrationState>();
        state.ActivationCommitted = true;   // 开启异步锁，防止重复点击
        Flash();
        EarnestModeButton.RequestRefresh();
        // 给予“认真模式”能力。若层数修改器把实际给予量降到了0、导致能力没有留下，则允许重新点击。
        await PowerCmd.Apply<EarnestModePower>(choiceContext, Owner, Amount, Owner, null);
        if (Owner.GetPower<EarnestModePower>() is null)
            state.ActivationCommitted = false;
        EarnestModeButton.RequestRefresh();
    }
    
    /// <summary>
    /// 认真模式结算完毕后由 EarnestModePower 显式通知。
    /// MotivationPower.Restart 是直接归零实现，不经过 PowerCmd，因此这里同步状态。
    /// </summary>
    public void NotifyEarnestModeEnded()
    {
        var state = GetInternalData<ConcentrationState>();
        state.ActivationCommitted = false;
        state.WasMotivationReady = false;
        EarnestModeButton.RequestRefresh();
    }
    
    public override Task AfterRemoved(Creature oldOwner)
    {
        EarnestModeButton.RequestRefresh();
        return Task.CompletedTask;
    }
    
    // 同步干劲的状态
    private void SyncMotivationState(bool flashWhenReady)
    {
        var state = GetInternalData<ConcentrationState>();
        var isReady = Owner.GetPower<MotivationPower>() is { IsCompleted: true };
        if (!isReady)
            // 干劲离开 100 后开始一个全新的可激活周期。
            state.ActivationCommitted = false;
        else if (flashWhenReady && state is { WasMotivationReady: false, ActivationCommitted: false })
            Flash();
        state.WasMotivationReady = isReady;
        EarnestModeButton.RequestRefresh();
    }
    
    private sealed class ConcentrationState
    {
        public bool ActivationCommitted;
        public bool WasMotivationReady;
    }
}