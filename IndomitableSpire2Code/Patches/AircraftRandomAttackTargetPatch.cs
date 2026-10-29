using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Animation;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Random;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// 原版在 AfterAttackerAnim/HitSfx 后才随机选目标，只为显式 ExecuteWithAircraftFlight
/// 登记的随机命令提前同一次抽取。普通命令与未登记的随机命令保持原逻辑；不自动安装演出。
/// 两处调用点仍需适配，避免复制原版 Execute 的伤害、钩子与多段实现。
/// </summary>
[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute), MethodType.Async)]
public static class AircraftRandomAttackTargetPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
        MethodBase __originalMethod)
    {
        var code = instructions.ToList();
        var targetMethod = AccessTools.Method(typeof(AttackCommand), "GetPossibleTargets");
        var randomMethod = AccessTools.Method(typeof(Rng), nameof(Rng.NextItem)).MakeGenericMethod(typeof(Creature));
        var commandField = __originalMethod.DeclaringType!.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(f => f.FieldType == typeof(AttackCommand));
        if (code.Count(c => c.Calls(targetMethod)) != 1 || code.Count(c => c.Calls(randomMethod)) != 1)
            throw new InvalidOperationException("Aircraft flight: AttackCommand target selection call sites changed.");
        foreach (var instruction in code)
        {
            if (instruction.Calls(targetMethod))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(AircraftAttackPresentation), nameof(AircraftAttackPresentation.GetTargets));
            }
            else if (instruction.Calls(randomMethod))
            {
                var load = new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction).MoveBlocksFrom(instruction);
                yield return load;
                yield return new CodeInstruction(OpCodes.Ldfld, commandField);
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(AircraftAttackPresentation), nameof(AircraftAttackPresentation.ConsumeRandomTarget));
            }
            yield return instruction;
        }
    }
}