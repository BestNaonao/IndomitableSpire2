using MegaCrit.Sts2.Core.Entities.Creatures;

namespace IndomitableSpire2.IndomitableSpire2Code.Abstracts;

public interface IAfterShieldBrokenSubscriber
{
    Task AfterShieldBroken(Creature target);
}