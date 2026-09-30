using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatVictory))]
public static class IndomitableVictoryAnimationPatch
{
    [HarmonyPostfix]
    private static void Postfix(ICombatState? combatState, ref Task __result)
    {
        __result = PlayVictoryAnimation(__result, combatState);
    }
    
    private static async Task PlayVictoryAnimation(Task originalTask, ICombatState? combatState)
    {
        // 等待胜利结算完成，再为不挠（含全部皮肤）播放一次 victory，结束后回到原有 Idle。
        // 直接使用动画入口，不读取卡牌动画开关，也不播放卡牌台词/音效。
        await originalTask;
        if (combatState == null) return;
        foreach (var player in combatState.Players)
        {
            if (player is { Character: Indomitable, Creature.IsAlive: true })
                await player.TriggerCardAnimation("victory", waitTime: player.Character.CastAnimDelay, useDefaultSfx: false);
        }
    }
}