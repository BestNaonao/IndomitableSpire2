using System.Reflection.Emit;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>拦截已经确定要执行的消耗，不靠攻击前的耐久预算猜测是否会被消耗。</summary>
[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Exhaust))]
public static class AfterglowExhaustPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal,
        bool skipVisuals, ref Task __result)
    {
        if (CombatManager.Instance.IsOverOrEnding) return true;
        if (AfterglowContext.ActiveCards.TryGetValue(card, out var active))
        {
            // 即使追加打出期间失去回光，也必须完成原消耗。追加中的消耗请求不能再次嵌套。
            if (active.FinalizingExhaust) return true;
            __result = Task.CompletedTask;
            return false;
        }
        if (!card.Keywords.Contains(IndomitableKeywords.Afterglow)) return true;
        // 原打出剩余的重放不能再次消耗已经进入消耗堆的牌。
        if (card.Pile?.Type == PileType.Exhaust)
        {
            __result = Task.CompletedTask;
            return false;
        }
        var data = new AfterglowContext.Data();
        AfterglowContext.ActiveCards.Add(card, data);
        __result = PlayBeforeExhaust(choiceContext, card, data, causedByEthereal, skipVisuals);
        return false;
    }
    
    private static async Task PlayBeforeExhaust(PlayerChoiceContext choiceContext, CardModel card,
        AfterglowContext.Data data, bool causedByEthereal, bool skipVisuals)
    {
        try
        {
            var target = card.CurrentTarget is { IsAlive: true } current ? current : null;
            await CardCmd.AutoPlay(choiceContext, card, target, skipCardPileVisuals: skipVisuals);
        }
        finally
        {
            data.PlayingBeforeExhaust = false;
            data.FinalizingExhaust = true;
            try { await CardCmd.Exhaust(choiceContext, card, causedByEthereal, skipVisuals); }
            finally { AfterglowContext.ActiveCards.Remove(card); }
        }
    }
}

// 回光严格追加一次；不重新消耗复制药水/自拍等次数修正，也不再展开固有重放。
[HarmonyPatch(typeof(CardModel), "GeneratePlayCount")]
public static class AfterglowReplayCountPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CardModel __instance, ref Task<int> __result)
    {
        if (!AfterglowContext.IsPlaying(__instance)) return true;
        __result = Task.FromResult(1);
        return false;
    }
}

/// <summary>回光追加打出暂留打出区，交回原消耗命令完成唯一一次入堆和消耗钩子。</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper), MethodType.Async)]
public static class AfterglowResultLocationPatch
{
    private static readonly System.Reflection.MethodInfo ResultLocationMethod =
        AccessTools.Method(typeof(CardModel), "GetResultLocationForCardPlay");
    private static readonly Func<CardModel, CardLocation> OriginalResultLocation =
        AccessTools.MethodDelegate<Func<CardModel, CardLocation>>(ResultLocationMethod);
    
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var wrapper = AccessTools.Method(typeof(AfterglowResultLocationPatch), nameof(GetResultLocation));
        if (code.Any(instruction => instruction.Calls(wrapper))) return code;
        // 在调用点包装虚方法，兼容卡牌自己的牌堆去向重写；不逐一给卡牌子类打补丁。
        return new CodeMatcher(code)
            .MatchStartForward(CodeMatch.Calls(ResultLocationMethod))
            .ThrowIfInvalid("Afterglow: GetResultLocationForCardPlay call not found in OnPlayWrapper")
            .Set(OpCodes.Call, wrapper)
            .InstructionEnumeration();
    }
    
    private static CardLocation GetResultLocation(CardModel card) => AfterglowContext.IsPlaying(card)
        ? new CardLocation(card.Owner, PileType.Play, CardPilePosition.Bottom)
        : OriginalResultLocation(card);
}