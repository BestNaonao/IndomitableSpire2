using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetUpCombat))]
public static class PatchCombatManagerSetUpCombat
{
    [HarmonyPostfix]
    public static void Postfix(CombatState state)
    {
        // 此时，战斗大框架已搭建完毕，且所有玩家的 PlayerCombatState 已经成功初始化
        foreach (var player in state.Players)
        {
            if (player is { Character: Indomitable, Creature.IsAlive: true })
            {
                MainFile.Logger.Info("Harmony Patch: Indomitable's CombatState is fully populated. Initializing Motivation.");
                // 由于原方法是同步的，使用 RunSafely 是最规范的做法
                // 它会在主线程安全地将我们的异步初始化任务加入引擎队列
                TaskHelper.RunSafely(InitMotivation(player.Creature));
            }
        }
    }
    
    private static async Task InitMotivation(Creature creature)
    {
        // 先 +1，触发 AfterApplied 自动补齐底数，此时 Amount=2, 显示=1
        await PowerCmd.Apply<MotivationPower>(
            new ThrowingPlayerChoiceContext(), creature, 1, creature, null, silent: true);
        // 再 -1，触发正常的递减，此时 Amount=1, 显示=0。// 达到拥有能力图标且数字为 0 的状态，且绝对早于任何进房遗物的触发！
        await PowerCmd.Apply<MotivationPower>(
            new ThrowingPlayerChoiceContext(), creature, -1, creature, null, silent: true);
    }
}