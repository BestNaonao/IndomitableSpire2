using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Combat;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Escape))]
public static class CreatureCmdEscapePatch
{
    // 在逃跑逻辑执行前触发，将即将丢失的 CombatState 抢救保存到 __state 中
    [HarmonyPrefix]
    public static void Prefix(Creature creature, out ICombatState? __state)
    {
        __state = creature.CombatState;
    }
    
    // 在逃跑逻辑执行后触发，接收 prefix 传过来的 __state
    [HarmonyPostfix]
    public static void Postfix(Creature creature, ICombatState? __state)
    {
        // 此时 creature.CombatState 虽然已经是 null 了，但我们有备份的 __state！
        if (__state != null) TaskHelper.RunSafely(CustomHook.AfterCreatureEscaped(creature, __state));
    }
}