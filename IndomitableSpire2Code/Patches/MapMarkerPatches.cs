using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>为原版各类地图节点统一挂载已注册的标记视图，不承担遗物生命周期或标记生成。</summary>
[HarmonyPatch]
public static class MapMarkerPatches
{
    [HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint._Ready)), HarmonyPostfix]
    private static void Normal(NNormalMapPoint __instance) => NCustomMapMarker.Attach(__instance);
    
    [HarmonyPatch(typeof(NBossMapPoint), nameof(NBossMapPoint._Ready)), HarmonyPostfix]
    private static void Boss(NBossMapPoint __instance) => NCustomMapMarker.Attach(__instance);
    
    [HarmonyPatch(typeof(NAncientMapPoint), nameof(NAncientMapPoint._Ready)), HarmonyPostfix]
    private static void Ancient(NAncientMapPoint __instance) => NCustomMapMarker.Attach(__instance);
}