using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch]
public static class IndomitableIdleAnimationPatch
{
    [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
    [HarmonyPostfix]
    private static void AttachTimer(NCreature __instance) => IndomitableIdleAnimation.Attach(__instance);
    
    [HarmonyPatch(typeof(FlavorSynchronizer), "CreateEndTurnPingDialogueIfNecessary")]
    [HarmonyPostfix]
    private static void PlayOnEndTurnPing(Player player)
    {
        // 原版发送端和接收端共用此入口，且发送端已做防抖；无需新增消息或匹配翻译后的气泡文本。
        // 只影响本机的对应角色；其他玩家是否播放由各自本机配置决定。
        if (!IndomitableConfiguration.PlayIdleAnimationOnEndTurnPing ||
            NRun.Instance == null || NCombatRoom.Instance == null) return;
        if (player.Creature.GetCreatureNode() is { } creature) IndomitableIdleAnimation.TryPlay(creature);
    }
}