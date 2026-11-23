using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using MegaCrit.Sts2.Core.Saves;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

/// <summary>
/// 处理不挠的连胜或断链动画，解决 Pending-DreadWinStreakResult 和按钮角色属于不同皮肤的问题。
/// 补丁在 InitChainForCharacter 中将本次动画查询参数对齐到待处理结果的 ID，原版便能匹配并播放。大厅里选中的赛车模型照常用于出战。
/// </summary>
[HarmonyPatch(typeof(NDreadScreen), "InitChainForCharacter")]
internal static class DreadSkinChainPatch
{
    // 普通不挠的结算使用共享进度 ID。原版以 ID 判断是否播放断链或连胜动画。
    [HarmonyPrefix]
    private static void Prefix(ref CharacterModel characterModel, StartRunLobby ____lobby)
    {
        if (characterModel is not Indomitable)
            return;
        var progress = SaveManager.Instance.Progress;
        var pending = ____lobby.NetService.Type == NetGameType.Singleplayer
            ? progress.PendingSingleplayerDreadWinStreakResult : progress.PendingMultiplayerDreadWinStreakResult;
        if (pending != null && IndomitableSharedProgressionPatch.GetProgressionId(pending.CharacterId) == ModelDb.GetId<IndomitableDefault>())
            characterModel = ModelDb.GetById<CharacterModel>(pending.CharacterId);
    }
}