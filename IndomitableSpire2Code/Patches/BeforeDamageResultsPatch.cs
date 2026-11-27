using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// 在原版 CreatureCmd.Damage 底层方法的 AfterModifyingDamageAmount 之后插入 CustomHook.BeforeDamageResults 方法。
/// </summary>
[HarmonyPatch]
internal static class BeforeDamageResultsPatch
{
    // 定义目标方法：使用 AccessTools.AsyncMoveNext 定位异步方法底层的状态机 MoveNext 方法
    private static MethodInfo TargetMethod() => AccessTools.AsyncMoveNext(AccessTools.Method(typeof(CreatureCmd),
        nameof(CreatureCmd.Damage), [typeof(PlayerChoiceContext), typeof(IEnumerable<DamageTargetData>), typeof(decimal),
            typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay), typeof(ModifyDamageHookType)]));
    
    // 复用原生 await；不新增状态机分支，也不改变原版受击前监听的时点。
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var code = instructions.ToList();
        var wrapper = AccessTools.Method(typeof(BeforeDamageResultsPatch), nameof(AwaitModifiersThenDispatch));
        if (code.Any(i => i.Calls(wrapper))) return code; // 幂等性检查
        var afterModifiers = AccessTools.Method(typeof(Hook), nameof(Hook.AfterModifyingDamageAmount));
        var calculate = AccessTools.Method(typeof(CreatureCmd), "GetDamageResults");
        var after = code.FindIndex(i => i.Calls(afterModifiers)); // 找到调用 afterModifiers 的索引
        var results = code.FindIndex(i => i.Calls(calculate)); // 找到调用 calculate 的索引
        // 健壮性校验：确保两个调用点存在，并且相对位置正确，并且都是最后（唯一）一次调用
        if (after < 0 || results <= after || after != code.FindLastIndex(i => i.Calls(afterModifiers)) ||
            results != code.FindLastIndex(i => i.Calls(calculate)))
            throw new InvalidOperationException("CreatureCmd.Damage 的修正通知与伤害试算调用点已改变。");
        // 动态构建参数加载 IL：
        // wrapper.GetParameters().Skip(1) 跳过第一个参数 (Task modifiers)，获取后续所有需要的上下文参数。
        // 其余参数名对应原方法的参数或跨 await 保存的局部变量。对每个参数，使用 AsyncStateFields.StateField 找到其在状态机中的对应字段，
        // 然后使用 AsyncStateFields.Load 生成读取该字段的 IL 指令 (Ldarg_0 + Ldfld)。SelectMany 将所有这些 IL 指令展平为一个序列。
        var loads = wrapper.GetParameters().Skip(1).SelectMany(p =>
            AsyncStateFields.Load(AsyncStateFields.StateField(__originalMethod, p.Name!, p.ParameterType)));
        // 执行注入：在 after (即 await Hook.AfterModifyingDamageAmount) 之后，插入加载参数的 IL 指令，
        // 最后插入调用 wrapper 方法的 IL 指令 (OpCodes.Call)。此时栈上已经按顺序准备好了 wrapper 方法所需的所有参数。
        code.InsertRange(after + 1, loads.Append(new CodeInstruction(OpCodes.Call, wrapper)));
        return code;
    }
    
    // 包装方法
    private static async Task AwaitModifiersThenDispatch(
        Task modifiers, PlayerChoiceContext choiceContext, Creature originalTarget,
        decimal modifiedAmount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        await modifiers;
        await CustomHook.BeforeDamageResults(choiceContext, 
            new PendingHit(originalTarget, modifiedAmount, props, dealer, cardSource, cardPlay));
    }
}