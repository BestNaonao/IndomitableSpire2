using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

// 在命令开始前包裹整个序列，避免在原版已缓存叠加实例后才插入另一次 Apply。
[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.Apply), typeof(PlayerChoiceContext), typeof(PowerModel), 
    typeof(Creature), typeof(decimal), typeof(Creature), typeof(CardModel), typeof(bool))]
public static class PowerCmdApplyCountPatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        PlayerChoiceContext choiceContext, PowerModel power, Creature target, decimal amount,
        Creature? applier, CardModel? cardSource, bool silent, ref Task __result)
    {
        if (PowerApplyCountContext.TryConsumeNativeCommand(true, choiceContext, power, target, amount, applier, cardSource) ||
            !power.CanGenerateApplyCount(target, amount)) return true;
        __result = PowerApplySequence.Apply(choiceContext, power, target, amount, applier, cardSource, silent);
        return false;
    }
}

// 泛型 Apply<T> 对已有实例会直接调用 ModifyAmount，所以也要覆盖这个入口。
[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.ModifyAmount))]
public static class PowerCmdModifyApplyCountPatch
{
    [HarmonyPrefix]
    private static bool Prefix(PlayerChoiceContext choiceContext, PowerModel power, decimal offset,
        Creature? applier, CardModel? cardSource, bool silent, ref Task<int> __result)
    {
        // 保持原版 IsEnding 的提前返回，不在战斗结束时提前读取 power.Owner。
        if (CombatManager.Instance.IsEnding || PowerApplyCountContext.IsSuppressed) return true;
        var target = power.Owner;
        if (PowerApplyCountContext.TryConsumeNativeCommand(false, choiceContext, power, target, offset, applier, cardSource) ||
            !power.CanGenerateApplyCount(target, offset)) return true;
        __result = PowerApplySequence.ModifyAmount(choiceContext, power, target, offset, applier, cardSource, silent);
        return false;
    }
}

internal static class PowerApplySequence
{
    internal static async Task Apply(PlayerChoiceContext choiceContext, PowerModel power, 
        Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent)
    {
        var count = await power.GenerateApplyCount(choiceContext, target, amount, applier, cardSource);
        var template = count > 1 ? (PowerModel)power.ClonePreservingMutability() : null;
        using (PowerApplyCountContext.UseNativeCommand(true, choiceContext, power, target, amount, applier, cardSource))
        {
            await PowerCmd.Apply(choiceContext, power, target, amount, applier, cardSource, silent);
        }
        await ApplyRemaining(choiceContext, template, target, amount, applier, cardSource, silent, count);
    }
    
    internal static async Task<int> ModifyAmount(PlayerChoiceContext choiceContext, PowerModel power, 
        Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent)
    {
        var count = await power.GenerateApplyCount(choiceContext, target, amount, applier, cardSource);
        var template = count > 1 ? (PowerModel)power.ClonePreservingMutability() : null;
        int originalResult;
        using (PowerApplyCountContext.UseNativeCommand(false, choiceContext, power, target, amount, applier, cardSource))
        {
            originalResult = await PowerCmd.ModifyAmount(choiceContext, power, amount, applier, cardSource, silent);
        }
        await ApplyRemaining(choiceContext, template, target, amount, applier, cardSource, silent, count);
        // 原版返回的是本次修改计算出的 newAmount，不包含后续钩子造成的其它改量。
        return originalResult;
    }
    
    private static async Task ApplyRemaining(PlayerChoiceContext choiceContext, PowerModel? template, 
        Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent, int count)
    {
        if (template == null) return;
        using var scope = PowerApplyCountContext.SuppressCountGeneration();
        for (var i = 1; i < count; i++)
        {
            if (CombatManager.Instance.IsEnding || !target.CanReceivePowers) break;
            // 每包都从首包施加前的快照创建新实例，重新走原版的堆叠和数值修正。
            // await 覆盖人工制品的扣除/移除，即使首包 amount 被归零也继续下一包。
            var repeatedPower = (PowerModel)template.ClonePreservingMutability();
            await PowerCmd.Apply(choiceContext, repeatedPower, target, amount, applier, cardSource, silent);
        }
    }
}
