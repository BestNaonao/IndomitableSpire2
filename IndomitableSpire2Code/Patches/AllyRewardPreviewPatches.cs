using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch]
public static class AllyRewardPreviewPatches
{
    [HarmonyPatch(typeof(NCardPlay), nameof(NCardPlay._Ready))]
    [HarmonyPostfix]
    private static void Attach(NCardPlay __instance)
    {
        if (__instance.Holder.CardModel is IAllyRewardPreview &&
            __instance.GetNodeOrNull<AllyRewardPreviewController>(AllyRewardPreviewController.NodeName) == null)
            __instance.AddChild(new AllyRewardPreviewController { Name = AllyRewardPreviewController.NodeName });
    }
    
    // Cleanup 覆盖成功/取消；TryPlayCard 在真正排入出牌动作前即清掉预览。
    // 若直接移除整个拖拽节点，子控制器的 _ExitTree 仍负责清理队友身上的提示行。
    [HarmonyPatch(typeof(NCardPlay), "TryPlayCard")]
    [HarmonyPrefix]
    private static void BeforePlay(NCardPlay __instance) => Clear(__instance);
    
    [HarmonyPatch(typeof(NCardPlay), "Cleanup")]
    [HarmonyPrefix]
    private static void BeforeCleanup(NCardPlay __instance) => Clear(__instance);
    
    private static void Clear(NCardPlay play) =>
        play.GetNodeOrNull<AllyRewardPreviewController>(AllyRewardPreviewController.NodeName)?.Stop();
}
