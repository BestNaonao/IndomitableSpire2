using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NCardPlay))]
internal static class ArmorBreakPreviewPatch
{
    [HarmonyPostfix, HarmonyPatch("OnCreatureHover")] // 鼠标悬停在敌人上时，与原版单目标选择一致
    private static void Hover(NCardPlay __instance, NCreature creature)
    {
        if (__instance.Holder.CardModel?.TargetType == TargetType.AnyEnemy && creature.Entity.IsEnemy)
            Controller(__instance).ShowTargets(creature);
    }
    
    [HarmonyPostfix, HarmonyPatch("ShowMultiCreatureTargetingVisuals")] // 原版多目标选择时机
    private static void ShowGroup(NCardPlay __instance)
    {
        if (__instance.Holder.CardModel?.TargetType is TargetType.AllEnemies or TargetType.RandomEnemy)
            Controller(__instance).ShowTargets(null); // null 表示群体模式
    }
    
    [HarmonyPrefix, HarmonyPatch("OnCreatureUnhover")] // 鼠标从敌人上移开时
    private static void Unhover(NCardPlay __instance, NCreature __0) =>
        Find(__instance)?.ClearSingleTarget(__0); // __0 代表原方法的第一个参数 (creature)
    
    [HarmonyPrefix, HarmonyPatch("HideTargetingVisuals")] // 隐藏目标视觉效果时，清理所有预览
    private static void Hide(NCardPlay __instance) => Find(__instance)?.Clear();
    
    [HarmonyPrefix, HarmonyPatch("TryPlayCard")] // 玩家真正点击打出卡牌前，清理预览（因为即将进入真实的伤害结算流程）
    private static void BeforePlay(NCardPlay __instance) => Find(__instance)?.Clear();
    
    private static ArmorBreakPreviewController? Find(NCardPlay play) =>
        play.GetNodeOrNull<ArmorBreakPreviewController>(nameof(ArmorBreakPreviewController));
    
    private static ArmorBreakPreviewController Controller(NCardPlay play)
    {
        if (Find(play) is { } existing) return existing; // 获取已有的
        var controller = new ArmorBreakPreviewController { Name = nameof(ArmorBreakPreviewController) }; // 懒创建
        play.AddChild(controller);
        return controller;
    }
}