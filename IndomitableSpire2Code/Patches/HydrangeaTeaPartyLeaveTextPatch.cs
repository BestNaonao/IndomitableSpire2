using System.Reflection;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Events;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// 为茶会结尾的原版离开按钮提供专属文本，保留原版完成状态、联机同步和打开地图的行为。
/// </summary>
[HarmonyPatch]
public static class HydrangeaTeaPartyLeaveTextPatch
{
    /// <summary>
    /// 标题与效果描述使用同一选项键，因此只需在两处本地化查询入口重定向键名。
    /// </summary>
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(EventModel), nameof(EventModel.GetOptionTitle));
        yield return AccessTools.Method(typeof(EventModel), nameof(EventModel.GetOptionDescription));
    }
    
    /// <summary>
    /// 仅替换已完成茶会的 PROCEED 文本；按钮仍由 NEventRoom 按原版流程创建并处理点击。
    /// </summary>
    [HarmonyPrefix]
    private static void Prefix(EventModel __instance, ref string key)
    {
        if (__instance is HydrangeaTeaParty && __instance.IsFinished && key == "PROCEED")
        {
            key = $"{__instance.Id.Entry}.pages.END.options.LEAVE";
        }
    }
}