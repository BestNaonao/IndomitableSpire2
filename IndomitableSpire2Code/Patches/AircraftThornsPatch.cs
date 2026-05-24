using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(ThornsPower), nameof(ThornsPower.BeforeDamageReceived))]
public static class AircraftThornsPatch
{
    [HarmonyPrefix]
    public static bool Prefix(ThornsPower __instance, ref Task __result, CardModel? cardSource)
    {
        // 如果伤害来源不是舰载机，则正常执行原版的荆棘反伤
        if (!cardSource.IsCarrierAircraft()) return true;
        // 【核心修复】：塞入一个已完成的 Task，防止外部 await null 导致崩溃，然后返回 false，拦截原版的反伤逻辑
        __result = Task.CompletedTask;
        return false;
    }
}