using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(CombatState), nameof(CombatState.AddPlayer))]
public static class PatchCombatStateAddPlayer
{
    public static void Postfix(CombatState __instance, Player player)
    {
        // 当有玩家被加入战斗状态时触发
        if (player.Character is not IndomitableCharacter) return;
        var targetCreature = player.Creature;
        if (targetCreature.IsDead) return;
        MainFile.Logger.Info("Harmony Patch: Indomitable entered combat!");
        // 同样使用 TaskHelper 来执行异步命令
        TaskHelper.RunSafely(PowerCmd.Apply<MotivationPower>(targetCreature, 1, targetCreature, null));
    }
}