using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>
/// 自定义战斗钩子：当 <c>CreatureCmd.LoseBlock</c> 实际移除格挡后触发。
/// </summary>
public interface IAfterBlockLostSubscriber
{
    /// <summary>
    /// 是否接收指定来源的格挡损失。默认忽略为落实格挡保留上限而进行的维护性裁剪。
    /// </summary>
    bool ShouldReceiveAfterBlockLost(BlockLossReason reason) => reason == BlockLossReason.Normal;

    /// <param name="choiceContext">原 LoseBlock 指令的玩家选择上下文。</param>
    /// <param name="target">失去格挡的生物。</param>
    /// <param name="amount">指令实际移除的格挡值，而不是请求移除值。</param>
    /// <param name="remover">导致格挡损失的生物；没有明确来源时为 null。</param>
    /// <param name="reason">本次 LoseBlock 的语义来源。</param>
    Task AfterBlockLost(
        PlayerChoiceContext choiceContext,
        Creature target,
        int amount,
        Creature? remover,
        BlockLossReason reason);
}