using System.Reflection;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// 原版模型钩子没有“地图已打开/关闭”的通知。只补充这两个 UI 入口，不改地图行为，不轮询，也不在此修改任何效果状态。
/// Close 的动画尚未结束时 IsOpen 已是 false，因而回到房间的过程中就能停止持续脉冲。
/// </summary>
[HarmonyPatch]
public static class RoyalViolinMapStatusPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
    [
        AccessTools.DeclaredMethod(typeof(NMapScreen), nameof(NMapScreen.Open)),
        AccessTools.DeclaredMethod(typeof(NMapScreen), nameof(NMapScreen.Close))
    ];
    
    [HarmonyPostfix]
    private static void Postfix(NMapScreen __instance, RunState? ____runState)
    {
        // Harmony 的 ___field 注入读取 NMapScreen._runState；额外的下划线来自原字段名。
        // 更新本机显示的所有玩家遗物，让查看队友背包时也遵循同样的地图/房间显示规则。
        if (____runState == null) return;
        foreach (var violin in ____runState.Players.SelectMany(player => player.Relics).OfType<RoyalViolin>())
            violin.RefreshMapStatus(__instance.IsOpen);
    }
}