using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Services;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>仅补充原版没有的本机地图选择模式；遗物状态、费用及房间变化都不在 UI 补丁中执行。</summary>
[HarmonyPatch]
public static class VacationMapPatches
{
    // 拦截 IsTravelable 属性的 get 方法，在非选择模式下返回原版逻辑，在选择模式下改为用 MapSelectionService 的 CanSelect 方法判断。
    [HarmonyPatch(typeof(NMapPoint), "get_IsTravelable"), HarmonyPrefix]
    private static bool Selectable(NMapPoint __instance, ref bool __result)
    {
        if (!VacationMapSelectionService.IsSelecting) return true;
        __result = VacationMapSelectionService.CanSelect(__instance.Point);
        return false;
    }
    
    // 拦截 OnMapPointSelectedLocally 方法，在非选择模式下返回原版逻辑，在选择模式下改为执行 MapSelectionService 的 Select 方法。
    [HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.OnMapPointSelectedLocally)), HarmonyPrefix]
    private static bool Select(NMapPoint point)
    {
        if (!VacationMapSelectionService.IsSelecting) return true;
        VacationMapSelectionService.Select(point.Point);
        return false; // 不进入原版地图投票/旅行；包括确认框显示期间的穿透点击。
    }
    
    // 状态安全保障。针对地图屏幕的生命周期关键节点（关闭、清理、重新设置地图、穿越旅行），强制关闭选择模式，确保状态不会泄露到下一个场景。
    [HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Close)), HarmonyPrefix]
    private static void Close() => VacationMapSelectionService.Cancel();
    [HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.CleanUp)), HarmonyPrefix]
    private static void CleanUp() => VacationMapSelectionService.Cancel();
    [HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap)), HarmonyPrefix]
    private static void SetMap() => VacationMapSelectionService.Cancel();
    [HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.TravelToMapCoord)), HarmonyPrefix]
    private static void Cancel() => VacationMapSelectionService.Cancel();
    
    // 每帧状态实时校验，失效时关闭选择模式。
    [HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen._Process)), HarmonyPostfix]
    private static void Validate() => VacationMapSelectionService.ValidateSession();
    
    // 有确认弹窗时，如果玩家按下取消键（如 ESc 或手柄 B 键），就关闭确认弹窗。
    [HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen._Input)), HarmonyPrefix]
    private static bool Input(NMapScreen __instance, InputEvent inputEvent)
    {
        if (!VacationMapSelectionService.HasConfirmation || !inputEvent.IsActionPressed(MegaInput.cancel)) return true;
        __instance.GetViewport().SetInputAsHandled(); // 告诉 Godot 引擎当前输入已被处理，不要再传给其他节点
        VacationMapSelectionService.DismissConfirmation();
        return false;
    }
}