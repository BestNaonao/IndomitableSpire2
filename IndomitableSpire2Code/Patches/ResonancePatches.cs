using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper), MethodType.Async)]
public static class ResonanceOnPlayPatch
{
    private static readonly MethodInfo OnPlayMethod = AccessTools.Method(typeof(CardModel), "OnPlay",
        [typeof(PlayerChoiceContext), typeof(CardPlay)]);
    private static readonly MethodInfo PlayInScopeMethod = AccessTools.Method(typeof(ResonanceOnPlayPatch), nameof(PlayInScope));
    private static readonly ConcurrentDictionary<MethodInfo, Func<CardModel, PlayerChoiceContext, CardPlay, Task>> PlayDelegates = new();
    
    [HarmonyTranspiler]
    [HarmonyPriority(Priority.Last)]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        // Harmony 重复注册同一个 transpiler 时，不再包装已经替换的入口。
        if (code.Any(instruction => instruction.Calls(PlayInScopeMethod))) return code;
        
        // RitsuLib 会先把原生 OnPlay 改为 RunCardOnPlayHooks。
        // 必须在它之后执行，并调用实际匹配到的入口，不能绕过其 Before/After 钩子。
        var sites = code.Select((instruction, index) => (instruction, index))
            .Where(site => site.instruction.Calls(OnPlayMethod) || IsRitsuOnPlayCall(site.instruction))
            .ToArray();
        if (sites.Length != 1)
            throw new InvalidOperationException($"Resonance: expected one native or RitsuLib OnPlay entry in OnPlayWrapper, found {sites.Length}.");
        
        var (call, index) = sites[0];
        var callback = (MethodInfo)call.operand;
        // 原调用的三个参数保持在栈上，额外传入入口 MethodInfo，交给异步包装器执行。
        CodeInstruction[] replacement =
        [
            new CodeInstruction(OpCodes.Ldtoken, callback).MoveLabelsFrom(call).MoveBlocksFrom(call),
            CodeInstruction.Call(typeof(MethodBase), nameof(MethodBase.GetMethodFromHandle), [typeof(RuntimeMethodHandle)]),
            new CodeInstruction(OpCodes.Castclass, typeof(MethodInfo)),
            new CodeInstruction(OpCodes.Call, PlayInScopeMethod)
        ];
        code.RemoveAt(index);
        code.InsertRange(index, replacement);
        return code;
    }
    
    private static bool IsRitsuOnPlayCall(CodeInstruction instruction) =>
        instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo
        {
            Name: "RunCardOnPlayHooks", IsStatic: true,
            DeclaringType.FullName: "STS2RitsuLib.Cards.CardOnPlayHook"
        } method && method.ReturnType == typeof(Task) && method.GetParameters().Select(parameter => parameter.ParameterType)
            .SequenceEqual([typeof(CardModel), typeof(PlayerChoiceContext), typeof(CardPlay)]);
    
    // 替换进去的包装方法。必须在调用原 OnPlay 之前进入作用域，并等待原 Task 真正完成。
    // 普通 Postfix 已经错过首个 await 之前的生成；Started/Finished 又无法覆盖异常退出。
    private static async Task PlayInScope(CardModel card, PlayerChoiceContext choiceContext, CardPlay cardPlay, MethodInfo callback)
    {
        using var scope = ResonanceContext.Enter(card);
        var invoke = PlayDelegates.GetOrAdd(callback,
            static method => AccessTools.MethodDelegate<Func<CardModel, PlayerChoiceContext, CardPlay, Task>>(method));
        await invoke(card, choiceContext, cardPlay);
    }
}

[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat))]
public static class ResonanceGeneratedCardsPatch
{
    [HarmonyPrefix]
    private static void Prefix(ref IEnumerable<CardModel> cards, PileType newPileType)
    {
        var source = ResonanceContext.Source;
        if (source == null || !CombatManager.Instance.IsInProgress || !newPileType.IsCombatPile()) return;
        // 将 IEnumerable 物化为 List。防止惰性求值（如 yield return）被枚举两次导致生成两批不同的卡。
        var generated = cards.ToList();
        cards = generated;  // 将 ref 参数替换为物化后的 List
        if (generated.Any(card => card.Pile != null)) return; // 交由原版报告非法生成。
        // 【核心】遍历所有新生成的卡牌，让它们继承“同心”来源的属性
        foreach (var card in generated) card.InheritResonanceFrom(source);
    }
}

[HarmonyPatch]
public static class ResonanceTransformPatch
{
    // 定位 CardCmd.Transform 的异步状态机（AsyncMoveNext）
    private static MethodInfo TargetMethod() => AccessTools.AsyncMoveNext(AccessTools.Method(
        typeof(CardCmd), nameof(CardCmd.Transform),
        [typeof(IEnumerable<CardTransformation>), typeof(Rng), typeof(CardPreviewStyle)]));
    
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var replacement = AccessTools.Method(typeof(ResonanceTransformPatch), nameof(GetReplacement));
        if (code.Any(instruction => instruction.Calls(replacement))) return code;
        return new CodeMatcher(code)
            // 查找获取转化后新卡牌的方法调用
            .MatchStartForward(CodeMatch.Calls(AccessTools.Method(typeof(CardTransformation), nameof(CardTransformation.GetReplacement))))
            .ThrowIfInvalid("Resonance: CardTransformation.GetReplacement call not found in Transform")
            .Set(OpCodes.Call, replacement)
            .InstructionEnumeration();
    }
    
    // 自定义的获取新卡方法
    private static CardModel? GetReplacement(ref CardTransformation transformation, Rng? rng)
    {
        // 只包装 Transform 内的调用，统一覆盖指定、随机和批量变化，不改动其它预览调用。
        var replacement = transformation.GetReplacement(rng);
        var original = transformation.Original;
        // 判断“同心”来源，如果找到了来源则让新卡继承属性。
        var source = original.Keywords.Contains(IndomitableKeywords.Resonance)
            ? original : ResonanceContext.Source;
        if (replacement != null && source != null) replacement.InheritResonanceFrom(source);
        return replacement;
    }
}