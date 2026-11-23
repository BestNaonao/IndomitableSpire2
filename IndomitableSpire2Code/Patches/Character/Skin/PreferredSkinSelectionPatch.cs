using System.Reflection;
using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

/// <summary>在自动选择或点击按钮选择角色前，按当前偏好刷新相关按钮。随后由原版完成选人和大厅同步。</summary>
[HarmonyPatch]
internal static class PreferredSkinSelectionPatch
{
    // 自动选择和手动点按钮都进入 SelectCharacter。
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        SkinLobbyHelper.ScreenMethods(nameof(ICharacterSelectButtonDelegate.SelectCharacter));
    
    // 先停止上一段选择语音，再给不挠或轮换角色的按钮刷新皮肤的角色模型和头像。
    [HarmonyPrefix]
    private static void Prefix(Control ____charButtonContainer, NCharacterSelectButton charSelectButton)
    {
        IndomitableVoice.Stop();
        if (charSelectButton.Character is Indomitable or RotatingCharacter)
            SkinLobbyHelper.RefreshSkinButtons(____charButtonContainer, SkinPreference.PreferredModel);
    }
}