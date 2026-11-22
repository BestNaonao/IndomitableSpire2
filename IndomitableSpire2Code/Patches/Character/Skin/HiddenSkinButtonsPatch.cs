using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

/// <summary>在极速、恐惧和自定义模式中隐藏多余皮肤的角色选择按钮，标准模式已经由 BaseLib 保障。需要注意 BaseLib 的后续更新。</summary>
[HarmonyPatch]
internal static class HiddenSkinButtonsPatch
{
    // 标准大厅由 BaseLib 过滤。其余大厅均先 Init，再根据容器中的按钮建立手柄导航。
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
    private static void RemoveHiddenButton(NCharacterSelectButton __instance, CharacterModel character, ICharacterSelectButtonDelegate del)
    {
        if (del is not (NBlitzScreen or NDreadScreen or NCustomRunScreen) ||
            character is not Indomitable { HideFromVanillaCharacterSelect: true })
            return;
        __instance.Hide();
        __instance.GetParent()?.RemoveChild(__instance);
        __instance.QueueFree();
    }
    
    // 恐惧大厅还会在 Init 返回后把按钮记入自己的列表，并根据 Visible 连接焦点，InitialFocusedControl 中也会访问第 0 个按钮。
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NDreadScreen), "InitCharacterButtons")]
    private static void RemoveHiddenDreadEntries(List<NCharacterSelectButton> ____charSelectButtons) =>
        ____charSelectButtons.RemoveAll(button => button.Character is Indomitable
        {
            HideFromVanillaCharacterSelect: true
        });
}