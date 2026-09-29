using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Combat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(Creature), nameof(Creature.BeforeTurnStart))]
public static class HypnotizedAnimationPatch
{
    [HarmonyPostfix]
    private static void Postfix(Creature __instance, CombatSide side)
    {
        // 不依赖能力是否还在：五层扣完移除能力后，下回合也必须能恢复待机。
        // BeforeTurnStart 只处理本次行动者，兼容联机中的个人额外回合。
        if (side == __instance.Side) HypnotizedAnimation.WakeAtTurnStart(__instance);
    }
}