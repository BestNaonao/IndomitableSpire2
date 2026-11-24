using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

/// <summary>
/// 开厅时尚未选人，隐藏上次的面板。结算时原版先换背景，再更新轮换头像。
/// </summary>
[HarmonyPatch(typeof(NDreadScreen), "InitRotatingCharacterButton")]
internal static class DreadRotationPanelWaitingPatch
{
    [HarmonyPostfix]
    private static void Postfix(NDreadScreen __instance, NCharacterSelectButton? ____selectedButton) =>
        InjectSkinSelectPanelPatch.RefreshPanel(__instance, ____selectedButton, __instance.Lobby);
}