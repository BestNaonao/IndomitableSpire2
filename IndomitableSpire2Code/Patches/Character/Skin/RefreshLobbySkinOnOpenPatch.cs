using System.Reflection;
using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

/// <summary>在选人大厅打开前为不挠的角色选择按钮刷新当前偏好皮肤，避免大厅节点复用导致的偏好展示过时。</summary>
[HarmonyPatch]
internal static class RefreshLobbySkinOnOpenPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        SkinLobbyHelper.ScreenMethods(nameof(NCharacterSelectScreen.OnSubmenuOpened));
    
    // 如果这个大厅上次显示的皮肤与当前偏好的皮肤不同，补丁在原版查找焦点前，把现有按钮换成当前偏好。即使这次原版自动选中战士，不挠头像也能正确刷新。
    [HarmonyPrefix]
    private static void Prefix(Control ____charButtonContainer) =>
        SkinLobbyHelper.RefreshFamilyButton(____charButtonContainer, SkinPreference.PreferredModel);
}