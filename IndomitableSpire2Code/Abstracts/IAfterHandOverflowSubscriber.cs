using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>自定义战斗钩子：卡牌因手牌已满被拒绝入手，改送操作成功后触发，不要求其最终仍在弃牌堆。</summary>
public interface IAfterHandOverflowSubscriber
{
    /// <param name="player">本次尝试接收卡牌的玩家。</param>
    /// <param name="card">实际发生溢出的卡牌；结算钩子时可能已被其他效果移动。</param>
    /// <param name="oldPileType">尝试入手前的牌堆，无原牌堆时为 None。</param>
    /// <param name="phase">容量判断发生时的玩家回合阶段。</param>
    Task AfterHandOverflow(Player player, CardModel card, PileType oldPileType, PlayerTurnPhase phase);
}