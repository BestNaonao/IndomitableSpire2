using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(ColorfulPhilosophers), "CardPoolColorOrder", MethodType.Getter)]
public sealed class TashkentPhilosophersPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<CardPoolModel> __result)
    {
        if (__result == null) return;

        var myModPools = ModelDb.AllCharacterCardPools
            .Where(static pool => pool is IPhilosophersCardPool);

        __result = __result
            .Concat(myModPools)
            .DistinctBy(static pool => pool.Id)
            .ToArray();
    }
}

public interface IPhilosophersCardPool { }