using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>一次命中一个目标，伤害乘区已结算，尚未计算格挡吸收和生命损失。</summary>
public readonly record struct PendingHit(Creature Target, decimal Damage, ValueProp Props,
    Creature? Dealer, CardModel? CardSource, CardPlay? CardPlay);

public interface IBeforeDamageResultsSubscriber
{
    // 先初始化本击的监听状态，再执行有副作用的受击效果。
    bool BeforeDamageResultsEarly => false;
    Task BeforeDamageResults(PlayerChoiceContext choiceContext, PendingHit hit);
}