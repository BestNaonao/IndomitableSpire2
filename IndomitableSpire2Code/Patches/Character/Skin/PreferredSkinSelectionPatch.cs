using System.Reflection;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

/// <summary>实现将皮肤偏好注入角色选择按钮的功能。</summary>
[HarmonyPatch]
internal static class PreferredSkinSelectionPatch
{
    // 目标是四个模式界面的原版的角色选择方法，不干涉 SkinSelectPanel 的切换皮肤功能。
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        SkinLobbyScreens.Methods(nameof(ICharacterSelectButtonDelegate.SelectCharacter));
    
    // 前缀截获选择按钮上的原角色，如果是不挠，就改为偏好皮肤，并记录按钮和原角色，作为“借条” 通过 __state 传递给 Finalizer。
    [HarmonyPrefix]
    private static void Prefix(NCharacterSelectButton charSelectButton, out SkinButtonModel? __state)
    {
        IndomitableVoice.Stop(); // 先暂停播放语音
        __state = charSelectButton is { IsLocked: false, Character: Indomitable }
            ? SkinButtonModel.Replace(charSelectButton, SkinPreference.PreferredModel)
            : null; // 空“借条”表示没有改变，后续也不需要恢复。
    }
    
    // 包括其他补丁或原版选人抛出异常的情况，避免把临时皮肤留在角色按钮上。
    [HarmonyFinalizer]
    private static void RestoreButton(SkinButtonModel? __state) => __state?.Restore();
}