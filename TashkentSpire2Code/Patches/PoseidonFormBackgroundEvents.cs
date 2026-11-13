using MegaCrit.Sts2.Core.Combat;
using TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

public static class PoseidonFormBackgroundEvents
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
    }

    private static void OnCombatSetUp(CombatState _) => NPoseidonFormVfx.Refresh();
}
