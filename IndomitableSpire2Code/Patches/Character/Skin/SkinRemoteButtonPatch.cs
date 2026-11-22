using System.Reflection;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

// 只改显示方法收到的玩家副本，网络消息与游戏存档继续使用实际皮肤。
[HarmonyPatch]
internal static class SkinRemoteButtonPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        SkinLobbyScreens.Methods("RefreshButtonSelectionForPlayer")
            .Concat(SkinLobbyScreens.Methods("RemotePlayerDisconnected"));
    
    [HarmonyPrefix]
    private static void Prefix(ref StartRunLobbyPlayer player)
    {
        if (player.character is Indomitable)
            player.character = ModelDb.Character<IndomitableDefault>();
    }
}