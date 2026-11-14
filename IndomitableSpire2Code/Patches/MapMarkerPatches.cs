using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>地图赋值时校验已存标记，为就绪的原版地图节点挂载标记布局层。</summary>
[HarmonyPatch]
public static class MapMarkerPatches
{
    // 存档可以在无人持有原遗物时仍有标记，校验必须发生在同步模型层。
    [HarmonyPatch(typeof(RunState), nameof(RunState.Map), MethodType.Setter), HarmonyPostfix]
    private static void OnMapAssigned(RunState __instance) => MapMarkerService.RevalidateMap(__instance, __instance.Map);
    
    [HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint._Ready)), HarmonyPostfix]
    private static void Normal(NNormalMapPoint __instance) => NMapMarkerLayer.Attach(__instance);
    
    [HarmonyPatch(typeof(NBossMapPoint), nameof(NBossMapPoint._Ready)), HarmonyPostfix]
    private static void Boss(NBossMapPoint __instance) => NMapMarkerLayer.Attach(__instance);
    
    [HarmonyPatch(typeof(NAncientMapPoint), nameof(NAncientMapPoint._Ready)), HarmonyPostfix]
    private static void Ancient(NAncientMapPoint __instance) => NMapMarkerLayer.Attach(__instance);
}