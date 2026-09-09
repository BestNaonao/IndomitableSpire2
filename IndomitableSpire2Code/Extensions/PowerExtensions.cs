using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class PowerExtensions
{
    /// <summary>
    /// 类似 CardModel.GeneratePlayCount：一次生成总次数并立即消费修正器配额。
    /// 只能在实际施加时调用；Single / None 不参与重复，额外包不重新生成次数。
    /// </summary>
    public static async Task<int> GenerateApplyCount(this PowerModel power, 
        PlayerChoiceContext choiceContext, Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (!power.CanGenerateApplyCount(target, amount)) return 1;
        var combatState = target.CombatState!;
        using var scope = PowerApplyCountContext.SuppressCountGeneration();
        var count = CustomHook.ModifyPowerApplyCount(
            combatState, power, target, amount, applier, cardSource, 1, out var modifyingModels);
        await CustomHook.AfterModifyingPowerApplyCount(
            combatState, choiceContext, power, target, amount, applier, cardSource, modifyingModels);
        return count;
    }
    
    public static bool CanGenerateApplyCount(this PowerModel power, Creature target, decimal amount) => 
        !PowerApplyCountContext.IsSuppressed && !CombatManager.Instance.IsEnding && 
        (!CombatManager.Instance.IsOverOrEnding || CombatManager.Instance.IsStarting) && 
        power.IsMutable && amount != 0 && power.StackType == PowerStackType.Counter && target.CanReceivePowers;
}
