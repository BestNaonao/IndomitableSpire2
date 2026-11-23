using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using MegaCrit.Sts2.Core.Saves;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

/// <summary>
/// 原版打开角色选择界面时会自动选择偏好的角色（历史进度中的上一个角色）的按钮。
/// 配合 RefreshLobbySkinOnOpenPatch，原版保存的偏好角色的 ID 和当前偏好的皮肤模型的 ID 不一致时，精确匹配会落空。
/// 该补丁识别出两者同属不挠，将焦点交给可见且已解锁的皮肤偏好的按钮。自定义大厅仍按原版选首项。
/// </summary>
[HarmonyPatch]
internal static class PreferredSkinFocusPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCharacterSelectScreen), "GetPreferredCharacterButton")]
    private static void RestoreStandardFocus(ref NCharacterSelectButton? __result,
        Control ____charButtonContainer, StartRunLobby ____lobby)
    {
        if (__result != null) return;
        var progress = SaveManager.Instance.Progress;
        var id = ____lobby.NetService.Type == NetGameType.Singleplayer
            ? progress.PreferredStandardCharacter : progress.PreferredMultiplayerStandardCharacter;
        __result = FindFamilyButton(id, ____charButtonContainer.GetChildren().OfType<NCharacterSelectButton>());
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NBlitzScreen), "GetPreferredCharacterButton")]
    private static void RestoreBlitzFocus(ref NCharacterSelectButton? __result,
        Control ____charButtonContainer, StartRunLobby ____lobby)
    {
        if (__result != null) return;
        var progress = SaveManager.Instance.Progress;
        var id = ____lobby.NetService.Type == NetGameType.Singleplayer
            ? progress.PreferredBlitzCharacter : progress.PreferredMultiplayerBlitzCharacter;
        __result = FindFamilyButton(id, ____charButtonContainer.GetChildren().OfType<NCharacterSelectButton>());
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NDreadScreen), nameof(NDreadScreen.OnSubmenuOpened))]
    private static void RestoreDreadFocus(ref Control? ____overriddenFocusedControl,
        List<NCharacterSelectButton> ____charSelectButtons, StartRunLobby ____lobby)
    {
        if (____overriddenFocusedControl != null) return;
        // 原版已处理轮换结果，鼠标的延迟选择与手柄取焦都在此后读取该控件。
        var progress = SaveManager.Instance.Progress;
        var id = ____lobby.NetService.Type == NetGameType.Singleplayer
            ? progress.PreferredDreadCharacter : progress.PreferredMultiplayerDreadCharacter;
        ____overriddenFocusedControl = FindFamilyButton(id, ____charSelectButtons);
    }
    
    private static NCharacterSelectButton? FindFamilyButton(ModelId id, IEnumerable<NCharacterSelectButton> buttons) =>
        IndomitableSharedProgressionPatch.GetProgressionId(id) != ModelDb.GetId<IndomitableDefault>()
            ? null
            : buttons.FirstOrDefault(button => button is { Visible: true, IsLocked: false, Character: Indomitable });
}