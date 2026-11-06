using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch]
// 仅处理原版没有模型钩子的 UI 注入和遭遇预定入口。
// 地图生成、进入房间前后由 EyeInTheSky 自己覆写原版钩子，不能在此补丁 Hook 调度器。
public static class EyeInTheSkyPatches
{
    [HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint._Ready)), HarmonyPostfix]
    private static void NormalMarker(NNormalMapPoint __instance) => EyeInTheSkyMarker.Attach(__instance);
    
    [HarmonyPatch(typeof(NBossMapPoint), nameof(NBossMapPoint._Ready)), HarmonyPostfix]
    private static void BossMarker(NBossMapPoint __instance) => EyeInTheSkyMarker.Attach(__instance);
    
    [HarmonyPatch(typeof(NAncientMapPoint), nameof(NAncientMapPoint._Ready)), HarmonyPostfix]
    private static void AncientMarker(NAncientMapPoint __instance) => EyeInTheSkyMarker.Attach(__instance);
    
    [HarmonyPatch(typeof(RunManager), "CreateRoom"), HarmonyPrefix]
    private static void Room(RunManager __instance, MapPointType mapPointType, ref AbstractModel? model)
    {
        if (model != null || !mapPointType.IsCombat() ||
            __instance.DebugOnlyGetState() is not { CurrentMapPoint: { } point } run ||
            EyeInTheSkyService.GetPlan(run, point) is not { } plan) return;
        model = EyeInTheSkyService.CreateEncounter(plan);
    }
    
    [HarmonyPatch(typeof(EncounterModel), nameof(EncounterModel.GenerateMonstersWithSlots)), HarmonyPrefix]
    private static void Monsters(EncounterModel __instance, IRunState runState) => EyeInTheSkyService.RestoreEncounterSeed(__instance, runState);
}