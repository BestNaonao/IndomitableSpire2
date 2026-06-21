using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

// 1. 【新增】：泛用型资源溢出监听接口
public interface IOnResourceOverflowSubscriber
{
    Task AfterResourceOverflowed(
        PlayerChoiceContext choiceContext,
        Creature target,            // 发生溢出的目标
        AbstractModel sourceModel,  // 导致溢出的源模型（比如 MotivationPower）
        decimal overflowAmount,     // 溢出量
        Creature? applier,          // 施加者
        CardModel? cardSource);     // 卡牌源
}