using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(ColorfulPhilosophers), "CardPoolColorOrder", MethodType.Getter)]
public sealed class IndomitablePhilosophersPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<CardPoolModel> __result)
    {
        // 直接通过类型抓取不挠的卡池
        var indomitablePool = ModelDb.AllCharacterCardPools.OfType<IndomitableCardPool>();
        // 将其追加到事件的可选项序列中，并根据 Id 去重
        __result = __result.Concat(indomitablePool).DistinctBy(static pool => pool.Id).ToArray();
    }
}