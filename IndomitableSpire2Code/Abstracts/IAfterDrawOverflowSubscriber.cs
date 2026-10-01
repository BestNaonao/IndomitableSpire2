using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>自定义战斗钩子：抽牌命令因手牌容量不足而留下未完成请求时触发。</summary>
public interface IAfterDrawOverflowSubscriber
{
    /// <param name="choiceContext">原抽牌命令的玩家选择上下文。</param>
    /// <param name="player">请求抽牌的玩家。</param>
    /// <param name="count">因容量不足未执行且当时有牌可抽的次数，以抽牌堆和弃牌堆的剩余总数为上限，不包含禁抽或缺牌造成的少抽。</param>
    /// <param name="fromHandDraw">是否来自原版回合开始抽牌。</param>
    /// <param name="phase">容量拒绝发生时的玩家回合阶段。</param>
    Task AfterDrawOverflow(PlayerChoiceContext choiceContext, Player player, int count,
        bool fromHandDraw, PlayerTurnPhase phase);
}