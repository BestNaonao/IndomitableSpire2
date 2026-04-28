using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

/// <summary>
/// 自定义钩子接口：当抽象模型的动态变量发生变化后触发
/// </summary>
public interface IAfterDynamicVarAmountChangedSubscriber
{
    public Task AfterDynamicVarAmountChanged(
        AbstractModel sourceModel,
        string variableName,
        decimal originalAmount,
        decimal offsetAmount,
        Creature target,
        Creature? applier);
}