using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(CombatState), nameof(CombatState.AddPlayer))]
public static class PatchCombatStateAddPlayer
{
    public static void Postfix(CombatState __instance, Player player)
    {
        // 当有玩家被加入战斗状态时触发
        if (player.Character is not Indomitable) return;
        var targetCreature = player.Creature;
        if (targetCreature.IsDead) return;
        MainFile.Logger.Info("Harmony Patch: Indomitable entered combat!");
        // 同样使用 TaskHelper 来执行异步命令
        TaskHelper.RunSafely(InitMotivation(targetCreature));
    }
    
    // 5. 将无声的 +1 / -1 组合封装在异步方法中
    private static async Task InitMotivation(Creature targetCreature)
    {
        // 先 +1，触发 AfterApplied 自动补齐底数，此时 Amount=2, 显示=1
        await PowerCmd.Apply<MotivationPower>(targetCreature, 1, targetCreature, null, silent: true);
        
        // 再 -1，触发正常的递减，此时 Amount=1, 显示=0。完美达到拥有能力图标且数字为 0 的状态！
        await PowerCmd.Apply<MotivationPower>(targetCreature, -1, targetCreature, null, silent: true);
    }
}