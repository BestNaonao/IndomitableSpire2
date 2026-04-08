using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Models.Powers; // 引入 PoisonPower

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
public static class BaseLibPoisonLethalFixPatch
{
    // 使用 High 优先级，确保它在普通的 Postfix（如 BaseLib 的）之前执行
    [HarmonyPostfix]
    [HarmonyPriority(Priority.High)]
    public static void Postfix(NHealthBar __instance)
    {
        var inst = Traverse.Create(__instance);
        var hpForeground = inst.Field("_hpForeground").GetValue<Control>();
        var creature = inst.Field("_creature").GetValue<MegaCrit.Sts2.Core.Entities.Creatures.Creature>();

        if (hpForeground != null && creature is { CurrentHp: > 0 })
        {
            var poison = creature.GetPower<PoisonPower>();
            var poisonDamage = poison?.CalculateTotalDamageNextTurn() ?? 0;

            // 【精准修复】：只有当单纯的【中毒伤害】就已经大于当前血量时，才去擦屁股
            // 这样就不会误伤到被灾厄隐藏，但实际上还留有宽度的红血条了
            if (poisonDamage < creature.CurrentHp) return;
            var maxFgWidth = inst.Property("MaxFgWidth").GetValue<float>();
            hpForeground.OffsetRight = -maxFgWidth;
        }
    }
}