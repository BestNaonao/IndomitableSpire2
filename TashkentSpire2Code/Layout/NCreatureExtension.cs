using MegaCrit.Sts2.Core.Nodes.Combat;
using TashkentSpire2.TashkentSpire2Code.Minion;

namespace TashkentSpire2.TashkentSpire2Code.Layout;

public static class NCreatureExtensions
{
    public static bool IsMinionNode(this NCreature node)
    {
        return node.Entity is { Monster: MinionModel, IsAlive: true, PetOwner: not null };
    }
}