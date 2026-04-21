using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Commands;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy))]
public static class PlayerCmdGainEnergyPatch
{
    // 1. 在原方法执行前，记录玩家当前的真实能量
    [HarmonyPrefix]
    public static void Prefix(Player player, out int __state)
    {
        __state = player.PlayerCombatState!.Energy;
    }

    // 2. 拦截原方法返回的 Task（通过 ref 关键字修改 __result）
    [HarmonyPostfix]
    public static void Postfix(ref Task __result, Player player, int __state)
    {
        // 将原 Task 替换为我们的异步包装方法
        __result = PostfixAsync(__result, player, __state);
    }

    // 3. 异步包装器：等待原方法完成 -> 计算差值 -> 触发异步钩子
    private static async Task PostfixAsync(Task originalTask, Player player, int energyBefore)
    {
        // 必须先等待原版的 GainEnergy 逻辑（包含其内部的动画和原版钩子）执行完毕
        await originalTask;

        // 获取执行后的实际能量
        var actualGain = player.PlayerCombatState!.Energy - energyBefore;

        // 如果真的获得了能量，则触发我们的自定义异步钩子
        if (actualGain > 0)
            await CustomHook.AfterEnergyGained(player, actualGain);
    }
}