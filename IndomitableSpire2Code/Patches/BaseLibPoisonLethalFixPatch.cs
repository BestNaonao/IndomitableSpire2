using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Models.Powers; // 引入 PoisonPower

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
public static class BaseLibPoisonLethalFixPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.High)]
    public static void Postfix(NHealthBar __instance)
    {
        var inst = Traverse.Create(__instance);
        var hpForeground = inst.Field("_hpForeground").GetValue<Control>();
        var creature = inst.Field("_creature").GetValue<MegaCrit.Sts2.Core.Entities.Creatures.Creature>();

        // 如果官方逻辑隐藏了红色血条，且怪物本身还没真死
        if (hpForeground != null && !hpForeground.Visible && creature is { CurrentHp: > 0 })
        {
            var poison = creature.GetPower<PoisonPower>();
            var poisonDamage = poison?.CalculateTotalDamageNextTurn() ?? 0;

            if (poisonDamage >= creature.CurrentHp) 
            {
                // 情况A：纯纯的中毒致死。修复昨天的 Bug（修正宽度，让 BaseLib 读到 0）
                var maxFgWidth = inst.Property("MaxFgWidth").GetValue<float>();
                hpForeground.OffsetRight = -maxFgWidth;
            }
            else 
            {
                // 情况B：中毒没致死，那是谁把血条隐藏的？是灾厄！
                // 为了防止 BaseLib v0.3.0.0 的 Visible 检查误判，我们强行把血条亮出来！
                // 别担心，BaseLib 算完自定义 DOT 后会接管并重新修正灾厄和隐藏逻辑。
                hpForeground.Visible = true;
            }
        }
    }
}