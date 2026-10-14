using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.TestSupport;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class CustomCardCmd
{
    /// <summary>保留原版移牌逻辑，只替换展示；返回实际移除数，供送餐啾计算最大生命值。</summary>
    public static async Task<int> RemoveFromDeckWithRelicPreview(IReadOnlyList<CardModel> cards, RelicModel source)
    {
        List<CardModel> removed = [];
        foreach (var card in cards)
        {
            // 前一张牌的移除钩子可能影响后续牌；不重复移除已离开牌组的卡牌。
            if (card.Pile?.Type != PileType.Deck) continue;
            await CardPileCmd.RemoveFromDeck(card, showPreview: false);
            removed.Add(card);
        }
        
        // 只有视觉效果在本机播放；双方都执行上面的原版命令，不以 LocalContext 决定游戏状态。
        if (!TestMode.IsOn && LocalContext.IsMe(source.Owner) && removed.Count > 0)
            _ = TaskHelper.RunSafely(PreviewRemovals(removed, source));
        return removed.Count;
    }
    
    private static async Task PreviewRemovals(IReadOnlyList<CardModel> cards, RelicModel source)
    {
        // CardPileCmd.RemoveFromDeck 的原生预览不暴露 NCard，无法给它附上来源遗物。
        // 因而只参考其“创建 NCard → 放大 → NCardRemoveVfx → 释放”展示段落。
        // NCardPreviewContainer 会自动将子节点横向居中排列；每批最多五张，避免大量甜点超出屏幕。
        foreach (var batch in cards.Chunk(5))
        {
            if (NRun.Instance == null) return;
            Tween? lastTween = null;
            foreach (var t in batch)
            {
                var node = NCard.Create(t);
                if (node == null) continue;
                NRun.Instance.GlobalUi.CardPreviewContainer.AddChildSafely(node);
                node.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
                var tween = node.CreateTween();
                lastTween = tween;
                tween.TweenProperty(node, "scale", Vector2.One * 0.7f, 0.25).From(Vector2.Zero)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
                // 与“吃不完的糖果”→ CardCreationResult.ModifyingRelics → NCardRewardSelectionScreen 相同的卡面闪烁。
                tween.TweenCallback(Callable.From(() => node.FlashRelicOnCard(source)));
                tween.TweenInterval(0.65);
                tween.TweenCallback(Callable.From(() =>
                    NRun.Instance.GlobalUi.AboveTopBarVfxContainer.AddChildSafely(NCardRemoveVfx.Create(node))));
                tween.TweenInterval(0.4);
                tween.TweenCallback(Callable.From(node.QueueFreeSafely));
            }
            // 等待实际动画而非 Cmd.Wait（快速模式会跳过它），保证下一批不会挤入尚未消失的卡牌。
            if (lastTween != null && !await lastTween.AwaitFinished(NRun.Instance)) return;
        }
    }
}