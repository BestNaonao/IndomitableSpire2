using System.Reflection;
using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Entities.Multiplayer;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

// 只改显示方法收到的玩家副本，网络消息与游戏存档继续使用实际皮肤。
[HarmonyPatch]
internal static class SkinRemoteButtonPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        SkinLobbyHelper.ScreenMethods("RefreshButtonSelectionForPlayer")
            .Concat(SkinLobbyHelper.ScreenMethods("RemotePlayerDisconnected"));
    
    [HarmonyPrefix]
    private static void Prefix(ref StartRunLobbyPlayer player, Control ____charButtonContainer)
    {
        if (player.character is Indomitable && SkinLobbyHelper.FindFamilyButton(____charButtonContainer) is { } button)
            player.character = button.Character;
    }
}