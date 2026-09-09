using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>
/// 修改一次能力请求的总重复施加次数（包含原始施加，最少为 1）。
/// 与原版卡牌重放一样，先生成次数并消费配额，再依次执行每个施加包。
/// amount 是未经数值修正的原始能力数量；此接口不用于预览。
/// </summary>
public interface IPowerApplyCountModifier
{
    int ModifyPowerApplyCount(
        PowerModel power, Creature target, decimal amount, Creature? applier, CardModel? cardSource, int applyCount);
    
    /// <summary>
    /// 仅通知实际改变次数的模型，每个请求一次，在第一包施加前调用。
    /// 此回调及额外施加产生的嵌套能力请求不再生成重复次数。
    /// </summary>
    Task AfterModifyingPowerApplyCount(
        PlayerChoiceContext choiceContext, PowerModel power, Creature target,
        decimal amount, Creature? applier, CardModel? cardSource);
}