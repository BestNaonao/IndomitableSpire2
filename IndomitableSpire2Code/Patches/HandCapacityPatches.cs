using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>集中定位当前原版的最终重载和异步状态机字段；结构不符时明确报错。</summary>
internal static class HandCapacityPatchTargets
{
    internal static MethodInfo Add => AccessTools.Method(typeof(CardPileCmd), nameof(CardPileCmd.Add),
        [typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool), typeof(bool)]);
    internal static MethodInfo Draw => AccessTools.Method(typeof(CardPileCmd), "DrawInternal",
        [typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool)]);
    
    /// <summary>同时按字段类型和原局部变量名定位，避免把不同循环或状态字段误认成目标。</summary>
    internal static FieldInfo StateField(MethodBase original, string name, Type type) =>
        original.DeclaringType!.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(field => field.FieldType == type && (field.Name == name || field.Name.StartsWith($"<{name}>")));
    
    /// <summary>生成从当前 MoveNext 状态机读取字段的 IL，不缓存可变指令实例。</summary>
    internal static IEnumerable<CodeInstruction> Load(FieldInfo field)
    {
        yield return new CodeInstruction(OpCodes.Ldarg_0);
        yield return new CodeInstruction(OpCodes.Ldfld, field);
    }
}

/// <summary>所有单张/批量、生成/移动 API 汇入唯一的最终 Add；只在这一层结算。</summary>
[HarmonyPatch]
public static class HandCapacityAddBoundaryPatch
{
    private static MethodInfo TargetMethod() => HandCapacityPatchTargets.Add;
    
    [HarmonyPostfix]
    private static void Postfix(ref Task<IReadOnlyList<CardPileAddResult>> __result) =>
        __result = AwaitAndBroadcast(__result);
    
    /// <summary>保留原任务的结果和异常，成功完成后才通过 CustomHook 逐张分发溢出。</summary>
    /// <param name="original">包含原动画和原版钩子的 Add 任务。</param>
    private static async Task<IReadOnlyList<CardPileAddResult>> AwaitAndBroadcast(Task<IReadOnlyList<CardPileAddResult>> original)
    {
        var result = await original;
        foreach (var overflow in HandCapacityCapture.TakeHandOverflows(result))
        {
            if (overflow.Scope is not { IsCurrent: true } scope) continue;
            await CustomHook.AfterHandOverflow(scope.CombatState, scope.Player, overflow.Card, overflow.OldPileType, scope.Phase);
        }
        return result;
    }
}

/// <summary>
/// 在实际异步状态机中观察满手分支，并包装唯一的 AddInternal 调用确认插入成功。
/// 不改原版目标牌堆、不重算上限，也不在同步插入过程中执行收益。
/// </summary>
[HarmonyPatch]
public static class HandCapacityAddCapturePatch
{
    private static MethodInfo TargetMethod() => AccessTools.AsyncMoveNext(HandCapacityPatchTargets.Add);
    
    [HarmonyTranspiler]
    [HarmonyPriority(Priority.Last)]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        var code = instructions.ToList();
        var capture = AccessTools.Method(typeof(HandCapacityCapture), nameof(HandCapacityCapture.AddAndRecord));
        if (code.Any(instruction => instruction.Calls(capture))) return code;
        var add = AccessTools.Method(typeof(CardPile), nameof(CardPile.AddInternal));
        var sites = code.Select((instruction, index) => (instruction, index))
            .Where(site => site.instruction.Calls(add)).ToArray();
        if (sites.Length != 1) throw new InvalidOperationException($"手牌容量补丁：预期一个 AddInternal 调用，实际找到 {sites.Length} 个。");
        var resultField = HandCapacityPatchTargets.StateField(original, "results", typeof(List<CardPileAddResult>));
        var indexField = HandCapacityPatchTargets.StateField(original, "i", typeof(int));
        var fullField = HandCapacityPatchTargets.StateField(original, "isFullHandAdd", typeof(bool));
        var fullAssignments = code.Select((instruction, index) => (instruction, index)).Where(site =>
            site.instruction.opcode == OpCodes.Stfld && Equals(site.instruction.operand, fullField)).ToArray();
        if (fullAssignments.Length != 1)
            throw new InvalidOperationException($"手牌容量补丁：预期一个满手判断，实际找到 {fullAssignments.Length} 个。");
        var site = sites[0];
        if (fullAssignments[0].index >= site.index)
            throw new InvalidOperationException("手牌容量补丁：满手判断必须位于实际添加之前。");
        // 栈上已有 pile、card、index、silent；只追加命令标识，将插入调用替换成记录包装器。
        var replacement = HandCapacityPatchTargets.Load(resultField).Concat(HandCapacityPatchTargets.Load(indexField))
            .Append(new CodeInstruction(OpCodes.Call, capture)).ToList();
        replacement[0].MoveLabelsFrom(site.instruction).MoveBlocksFrom(site.instruction);
        code.RemoveAt(site.index);
        code.InsertRange(site.index, replacement);
        // 判断后立即记录原牌堆和回合信息；后续移牌不能改写被拒绝入手的原因，监听者留到广播时枚举。
        var attempt = HandCapacityPatchTargets.Load(resultField).Concat(HandCapacityPatchTargets.Load(indexField))
            .Concat(HandCapacityPatchTargets.Load(fullField)).Append(new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(HandCapacityCapture), nameof(HandCapacityCapture.CaptureHandAttempt))));
        code.InsertRange(fullAssignments[0].index + 1, attempt);
        return code;
    }
}

/// <summary>只包装 DrawInternal 一层，避免单张 Draw 和批量 Draw 重复结算同一请求。</summary>
[HarmonyPatch]
public static class HandCapacityDrawBoundaryPatch
{
    private static MethodInfo TargetMethod() => HandCapacityPatchTargets.Draw;
    
    [HarmonyPostfix]
    private static void Postfix(ref Task<IEnumerable<CardModel>> __result) => __result = AwaitAndBroadcast(__result);
    
    /// <summary>等待原抽牌结束，随后将一次容量拒绝及有牌可抽的被拒次数交给 CustomHook。</summary>
    /// <param name="original">原版抽牌任务；禁抽、无牌及异常路径不会被改写成过量抽牌。</param>
    private static async Task<IEnumerable<CardModel>> AwaitAndBroadcast(Task<IEnumerable<CardModel>> original)
    {
        var result = await original;
        var overflow = HandCapacityCapture.TakeDrawOverflow(result);
        if (overflow is { Scope.IsCurrent: true })
        {
            var scope = overflow.Scope;
            await CustomHook.AfterDrawOverflow(scope.CombatState, overflow.ChoiceContext, scope.Player,
                overflow.Count, overflow.FromHandDraw, scope.Phase);
        }
        return result;
    }
}

/// <summary>保留原版容量/禁抽/洗牌判断，由记录方法排除无牌支持的剩余请求。</summary>
[HarmonyPatch]
public static class HandCapacityDrawCapturePatch
{
    private static MethodInfo TargetMethod() => AccessTools.AsyncMoveNext(HandCapacityPatchTargets.Draw);
    
    [HarmonyTranspiler]
    [HarmonyPriority(Priority.Last)]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        var code = instructions.ToList();
        var capacity = AccessTools.Method(typeof(HandCapacityCapture), nameof(HandCapacityCapture.RecordComputedCapacity));
        if (code.Any(instruction => instruction.Calls(capacity))) return code;
        var result = HandCapacityPatchTargets.StateField(original, "result", typeof(List<CardModel>));
        var player = HandCapacityPatchTargets.StateField(original, "player", typeof(Player));
        var requested = HandCapacityPatchTargets.StateField(original, "drawsRequested", typeof(int));
        var fromHandDraw = HandCapacityPatchTargets.StateField(original, "fromHandDraw", typeof(bool));
        var choiceContext = HandCapacityPatchTargets.StateField(original, "choiceContext", typeof(PlayerChoiceContext));
        var check = AccessTools.Method(typeof(CardPileCmd), "CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot");
        var max = AccessTools.Method(typeof(Math), nameof(Math.Max), [typeof(int), typeof(int)]);
        var computedSites = code.Select((instruction, index) => (instruction, index))
            .Where(site => site.instruction.Calls(max)).ToArray();
        var checkSites = code.Select((instruction, index) => (instruction, index))
            .Where(site => site.instruction.Calls(check)).ToArray();
        // 只匹配读取手牌后的容量比较，不能把循环次数比较也当作满手判断。
        var compareSites = code.Select((instruction, index) => (instruction, index))
            .Where(site => site.instruction.opcode == OpCodes.Bge || site.instruction.opcode == OpCodes.Bge_S)
            .Where(site => code.Take(site.index).Skip(Math.Max(0, site.index - 12)).Any(instruction =>
                instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand,
                    HandCapacityPatchTargets.StateField(original, "hand", typeof(CardPile)))))
            .ToArray();
        if (computedSites.Length != 2 || checkSites.Length != 3 || compareSites.Length != 1)
            throw new InvalidOperationException($"手牌容量补丁：DrawInternal 结构不符（容量计算={computedSites.Length}，可抽检查={checkSites.Length}，满手比较={compareSites.Length}）。");
        
        IEnumerable<CodeInstruction> DrawArguments() => HandCapacityPatchTargets.Load(result)
            .Concat(HandCapacityPatchTargets.Load(player)).Concat(HandCapacityPatchTargets.Load(requested))
            .Concat(HandCapacityPatchTargets.Load(fromHandDraw)).Concat(HandCapacityPatchTargets.Load(choiceContext));
        
        var replacements = new List<(int Index, int Remove, List<CodeInstruction> Code)>();
        // 第一次容量计算和每次成功抽牌后的容量计算均保留原值，记录方法只旁观停止原因。
        foreach (var site in computedSites)
            replacements.Add((site.index + 1, 0, DrawArguments().Append(new CodeInstruction(OpCodes.Call, capacity)).ToList()));
        foreach (var site in checkSites)
        {
            // 原 player 已在栈上，再加载抽牌信息及原选择上下文。
            var replacement = HandCapacityPatchTargets.Load(result).Concat(HandCapacityPatchTargets.Load(requested))
                .Concat(HandCapacityPatchTargets.Load(fromHandDraw)).Concat(HandCapacityPatchTargets.Load(choiceContext))
                .Append(new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(HandCapacityCapture), nameof(HandCapacityCapture.CheckDrawPossible)))).ToList();
            replacement[0].MoveLabelsFrom(site.instruction).MoveBlocksFrom(site.instruction);
            replacements.Add((site.index, 1, replacement));
        }
        foreach (var site in compareSites)
        {
            // 将原 >= 跳转转为等价布尔结果，记录后仍跳到原目标，不改变抽牌是否执行。
            var replacement = new List<CodeInstruction> {
                new(OpCodes.Clt), new(OpCodes.Ldc_I4_0), new(OpCodes.Ceq)
            };
            replacement.AddRange(DrawArguments());
            replacement.Add(new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(HandCapacityCapture), nameof(HandCapacityCapture.RecordFullHandComparison))));
            replacement.Add(new CodeInstruction(OpCodes.Brtrue, site.instruction.operand));
            replacement[0].MoveLabelsFrom(site.instruction).MoveBlocksFrom(site.instruction);
            replacements.Add((site.index, 1, replacement));
        }
        foreach (var replacement in replacements.OrderByDescending(item => item.Index))
        {
            // 从后向前插入，前面定位好的指令索引不会因后续替换而偏移。
            code.RemoveRange(replacement.Index, replacement.Remove);
            code.InsertRange(replacement.Index, replacement.Code);
        }
        return code;
    }
}

/// <summary>只记录原检查器的 HAND_FULL 分支，区分 NO_DRAW，并兼容其手牌上限补丁。</summary>
[HarmonyPatch(typeof(CardPileCmd), "CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot")]
public static class HandCapacityDrawCheckCapturePatch
{
    [HarmonyTranspiler]
    [HarmonyPriority(Priority.Last)]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var record = AccessTools.Method(typeof(HandCapacityCapture), nameof(HandCapacityCapture.RecordFullHandCheck));
        if (code.Any(instruction => instruction.Calls(record))) return code;
        var marker = code.FindIndex(instruction => instruction.opcode == OpCodes.Ldstr && Equals(instruction.operand, "HAND_FULL"));
        if (marker < 0) throw new InvalidOperationException("手牌容量补丁：未找到抽牌检查中的 HAND_FULL 分支。");
        var play = code.FindIndex(marker, instruction => instruction.Calls(AccessTools.Method(typeof(ThinkCmd), nameof(ThinkCmd.Play))));
        if (play < 0) throw new InvalidOperationException("手牌容量补丁：未找到 HAND_FULL 分支的提示调用。");
        code.InsertRange(play + 1, [new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Call, record)]);
        return code;
    }
}