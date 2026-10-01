using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class WhateverPower : IndomitablePower, IAfterHandOverflowSubscriber, IAfterDrawOverflowSubscriber
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];
    
    // 实际溢出的每一张牌对应一次收益，不要求它结算时仍位于弃牌堆。
    public Task AfterHandOverflow(Player player, CardModel card, PileType oldPileType, PlayerTurnPhase phase) =>
        Trigger(new ThrowingPlayerChoiceContext(), player, 1, phase);
    
    // 每次有牌可抽但被容量拒绝的请求各执行一次收益，不把 count 次合成一次大额效果。
    public Task AfterDrawOverflow(PlayerChoiceContext choiceContext, Player player, int count, bool fromHandDraw,
        PlayerTurnPhase phase) => Trigger(choiceContext, player, count, phase);
    
    // 沿用不休陀螺的稳定空手钩子及阶段限制，忽略弃牌重抽的中间空手。
    public override Task AfterHandEmptied(PlayerChoiceContext choiceContext, Player player)
    {
        var phase = player.PlayerCombatState?.Phase;
        return phase is PlayerTurnPhase.AutoPrePlay or PlayerTurnPhase.Play or PlayerTurnPhase.AutoPostPlay
            ? Trigger(choiceContext, player, 1, phase.Value) : Task.CompletedTask;
    }
    
    /// <summary>只允许自己参与的玩家回合；结束回合、死亡、能力移除后不再产生新收益。</summary>
    /// <param name="player">原事件对应玩家，必须是本能力拥有者。</param>
    /// <param name="phase">事件发生时的阶段，End 和 None 均不属于本能力的有效阶段。</param>
    private bool CanTrigger(Player player, PlayerTurnPhase phase) =>
        player == Owner.Player && Amount > 0 && !Owner.IsDead &&
        CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding &&
        CombatManager.Instance.IsPartOfPlayerTurn(player) &&
        phase is PlayerTurnPhase.Start or PlayerTurnPhase.AutoPrePlay or PlayerTurnPhase.Play or PlayerTurnPhase.AutoPostPlay;
    
    /// <summary>
    /// 逐次结算：每次开始时读取当前 Amount，分别治疗一次、施加一次原版下回合能量。
    /// 同一次收益的两个量保持一致；期间若层数改变，下一次收益重新读取 Amount。
    /// 每个 await 均位于原命令等待链内，原版能量能力负责累计、显示、发放及移除。
    /// </summary>
    /// <param name="choiceContext">收益施加能力时沿用的上下文，本方法不会创建独立玩家动作。</param>
    /// <param name="player">本次事件的玩家，不能替其他玩家结算收益。</param>
    /// <param name="count">收益次数，不是治疗量或能量总量。</param>
    /// <param name="phase">事件发生时的回合阶段；空手钩子传入稳定检查时的阶段。</param>
    private async Task Trigger(PlayerChoiceContext choiceContext, Player player, int count, PlayerTurnPhase phase)
    {
        if (!CanTrigger(player, phase)) return;
        var combatState = Owner.CombatState;
        var turnNumber = player.PlayerCombatState!.TurnNumber;
        for (var i = 0; i < count; i++)
        {
            // 防止前一次收益的嵌套效果结束战斗、移除本能力或切换回合后，继续处理旧事件。
            if (!CanTrigger(player, phase) || !ReferenceEquals(Owner.CombatState, combatState) ||
                player.PlayerCombatState?.TurnNumber != turnNumber) break;
            if (i == 0) Flash();
            // 记录 Amount，避免指令导致 Amount 变化。
            var reward = Amount;
            await CreatureCmd.Heal(Owner, reward);
            await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner, reward, Owner, null);
        }
    }
}