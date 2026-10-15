using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>将通用牌堆指令连接到原版 NCardHighlight；不创建另一套边框贴图或 shader。</summary>
[HarmonyPatch]
public static class CardPileGlowPatches
{
    private sealed class ViewState
    {
        public bool Applied;
        public Color PreviousColor;
    }
    
    private static readonly ConditionalWeakTable<NCard, ViewState> Views = new();
    private static readonly List<WeakReference<NCard>> LiveViews = [];
    
    static CardPileGlowPatches() => CustomCardPileCmd.GlowChanged += RefreshViews;
    
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    [HarmonyPostfix]
    private static void AfterUpdateVisuals(NCard __instance) => Refresh(__instance);
    
    // 也刷新仍打开着的大图：来源卡离开/返回目标牌堆时，大图本身不一定重新绘制。
    [HarmonyPatch(typeof(CardPile), nameof(CardPile.InvokeContentsChanged))]
    [HarmonyPostfix]
    private static void AfterPileChanged() => RefreshViews(null);
    
    // 手牌容器会在 UpdateVisuals 返回后重新设置原生可打出/金色/红色边框。
    // 通用指令也支持 Hand，所以在容器更新完后再应用显式指定的效果；没有指令时保留原版表现。
    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard))]
    [HarmonyPostfix]
    private static void AfterHandUpdate(NHandCardHolder __instance)
    {
        if (__instance.CardNode is { } card) Refresh(card, forceShow: true);
    }
    
    [HarmonyPatch(typeof(NCard), nameof(NCard.OnFreedToPool))]
    [HarmonyPrefix]
    private static void BeforeFreedToPool(NCard __instance) => ReleaseView(__instance);
    
    [HarmonyPatch(typeof(NCard), nameof(NCard.OnReturnedFromPool))]
    [HarmonyPrefix]
    private static void BeforeReturnedFromPool(NCard __instance) => ReleaseView(__instance);
    
    [HarmonyPatch(typeof(NCard), nameof(NCard._ExitTree))]
    [HarmonyPrefix]
    private static void BeforeExitTree(NCard __instance) => ReleaseView(__instance);
    
    private static void ReleaseView(NCard card)
    {
        if (Views.TryGetValue(card, out var state) && state.Applied && card.IsNodeReady())
        {
            card.CardHighlight.Modulate = state.PreviousColor;
            card.CardHighlight.AnimHideInstantly();
        }
        Views.Remove(card);
        LiveViews.RemoveAll(reference => !reference.TryGetTarget(out var target) || target == card);
    }
    
    private static void RefreshViews(CardModel? changedCard)
    {
        // 不持有已关闭界面的强引用；打开中的列表、大图及升级预览立即响应 Upsert/Remove。
        for (var i = LiveViews.Count - 1; i >= 0; i--)
        {
            if (!LiveViews[i].TryGetTarget(out var card) || !GodotObject.IsInstanceValid(card))
            {
                LiveViews.RemoveAt(i);
                continue;
            }
            if (changedCard == null || card.Model is { } model && CustomCardPileCmd.GetPreviewSource(model) == changedCard)
                Refresh(card);
        }
    }
    
    private static void Refresh(NCard card, bool forceShow = false)
    {
        if (!card.IsNodeReady() || !card.IsInsideTree()) return;
        if (!Views.TryGetValue(card, out var state))
        {
            state = new ViewState();
            Views.Add(card, state);
            LiveViews.Add(new WeakReference<NCard>(card));
        }
        if (card is { Visibility: ModelVisibility.Visible, Model: { } model } &&
            CustomCardPileCmd.TryGetGlow(model, out var color))
        {
            if (!state.Applied) state.PreviousColor = card.CardHighlight.Modulate;
            // 参考 NHandCardHolder.UpdateCard：Modulate 作为颜色滤镜，AnimShow 控制原 shader 的 width。
            card.CardHighlight.Modulate = color;
            if (!state.Applied || forceShow) card.CardHighlight.AnimShow();
            state.Applied = true;
        }
        else if (state.Applied)
        {
            state.Applied = false;
            card.CardHighlight.Modulate = state.PreviousColor;
            card.CardHighlight.AnimHideInstantly();
            // 移除显式效果后恢复手牌当前的原生判定；Applied 已清空，因此不会递归恢复。
            if (card.GetParent() is NHandCardHolder holder && holder.CardNode == card) holder.UpdateCard();
        }
    }
}

/// <summary>仅为查看大图及格子升级预览保留来源身份，不改变实际卡牌克隆的游戏语义。</summary>
[HarmonyPatch]
public static class CardPileGlowPreviewPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NInspectCardScreen), "UpdateCardDisplay");
        yield return AccessTools.Method(typeof(NGridCardHolder), "UpdateCardModel");
    }
    
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var clone = AccessTools.Method(typeof(AbstractModel), nameof(AbstractModel.MutableClone));
        var previewClone = AccessTools.Method(typeof(CardPileGlowPreviewPatch), nameof(CloneForPreview));
        if (code.Any(instruction => instruction.Calls(previewClone))) return code;
        return new CodeMatcher(code)
            .MatchStartForward(CodeMatch.Calls(clone))
            .ThrowIfInvalid("Card pile glow: UI preview clone call not found")
            .Set(OpCodes.Call, previewClone)
            .InstructionEnumeration();
    }
    
    private static AbstractModel CloneForPreview(AbstractModel source)
    {
        var preview = source.MutableClone();
        if (preview is CardModel card && source is CardModel original)
            CustomCardPileCmd.RegisterPreview(card, original);
        return preview;
    }
}