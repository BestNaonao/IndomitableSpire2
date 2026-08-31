using MegaCrit.Sts2.Core.Entities.Creatures;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

// 用于处理生物逃跑的接口
public interface IAfterCreatureEscapedSubscriber
{
    public Task AfterCreatureEscaped(Creature creature);
}