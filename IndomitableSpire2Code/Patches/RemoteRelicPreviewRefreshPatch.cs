using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NMultiplayerPlayerExpandedState), nameof(NMultiplayerPlayerExpandedState._Ready))]
public static class RemoteRelicPreviewRefreshPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMultiplayerPlayerExpandedState __instance, Player ____player, Control ____relicContainer)
    {
        // 监听器由玩家事件持有，在窗口离树时解除；不引入额外 Godot 脚本资源。
        _ = new RemoteRelicPreviewObserver(__instance, ____player, ____relicContainer);
    }
}