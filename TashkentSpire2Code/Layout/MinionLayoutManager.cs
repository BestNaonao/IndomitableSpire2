using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TashkentSpire2.TashkentSpire2Code.Layout;

public static class MinionLayoutManager
{
    private static readonly List<(IMinionLayout layout, int priority, int order)> LayoutsWithPriority = [];
    private static int _counter;

    static MinionLayoutManager()
    {
        Register(new DefaultMinionLayout());
    }

    public static IEnumerable<IMinionLayout> Layouts
        => LayoutsWithPriority.Select(x => x.layout);
    
    public static void Register(IMinionLayout layout, int priority = 0)
    {
        LayoutsWithPriority.Add((layout, priority, _counter++));

        LayoutsWithPriority.Sort((a, b) =>
        {
            var pComp = b.priority.CompareTo(a.priority);
            return pComp != 0 ? pComp : b.order.CompareTo(a.order);
        });
    }

    public static IEnumerable<MinionNodePosition> CalculateLayout(NCombatRoom room)
    {
        var context = new MinionLayoutContext(room);
        
        foreach (var layout in Layouts)
        {
            if (layout.IsActive)
            {
                layout.ApplyLayout(context);
            }
        }
        
        return context.Positions.Select(entry => new MinionNodePosition(entry.Key, entry.Value)).ToList();
    }

    public static IReadOnlyList<MinionNodePosition> GetCurrentMinionPositions(NCombatRoom room)
    {
        var minions = room.CreatureNodes.Where(n => n.IsMinionNode());
        return minions.Select(c => new MinionNodePosition(c, c.Position)).ToList();
    }
}