using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public class CustomCardPileCmd
{
    // 与原版 NCardHighlight 的 gold、red 完全相同。保留纯 Color 值，模型层无需初始化含 StringName 的 UI 类型。
    public static readonly Color PlayableGlowColor = new(0.0f, 0.957f, 0.988f, 0.98f);
    public static readonly Color GoldGlowColor  = new(1f, 0.784f, 0.0f, 0.98f);
    public static readonly Color RedGlowColor  = new(0.83f, 0.0f, 0.33f, 0.98f);
    
    private sealed record Glow(PileType Pile, Color Color);
    private sealed record Preview(CardModel Source);
    private static readonly ConditionalWeakTable<CardModel, Glow> Glows = new();
    private static readonly ConditionalWeakTable<CardModel, Preview> Previews = new();
    internal static event Action<CardModel>? GlowChanged;
    
    /// <summary>
    /// 为指定卡牌设置一个原版闪耀边框。仅在这张卡实际位于指定牌堆时显示，也支持尚未打开的牌堆界面。
    /// 每张卡只保留一个效果；再次调用会覆盖颜色及目标牌堆，不叠加节点或动画。这只是运行时的显示状态；离开指定牌堆时隐藏，返回时恢复。
    /// </summary>
    public static void UpsertGlow(CardModel card, PileType pile, Color color)
    {
        card.AssertMutable();
        if (pile == PileType.None) throw new ArgumentException("A glow requires an actual card pile.", nameof(pile));
        Glows.AddOrUpdate(card, new Glow(pile, color));
        GlowChanged?.Invoke(card);
    }
    
    /// <summary>移除该卡在指定牌堆上的效果；不匹配的牌堆不会误删后来 Upsert 的效果。</summary>
    public static bool RemoveGlow(CardModel card, PileType pile)
    {
        if (!Glows.TryGetValue(card, out var glow) || glow.Pile != pile) return false;
        Glows.Remove(card);
        GlowChanged?.Invoke(card);
        return true;
    }
    
    /// <summary>查询当前可显示的效果。预览只读取来源卡，不会把效果复制到战斗生成的新卡上。</summary>
    public static bool TryGetGlow(CardModel card, out Color color)
    {
        var source = GetPreviewSource(card);
        if (Glows.TryGetValue(source, out var glow) && source.Pile?.Type == glow.Pile)
        {
            color = glow.Color;
            return true;
        }
        color = default;
        return false;
    }
    
    // 只由 UI 预览克隆入口注册，不拦截所有 MutableClone，也不伪造 DeckVersion。试吃附魔同样可以据此查询真实来源卡的战斗历史。
    internal static void RegisterPreview(CardModel preview, CardModel source) =>
        Previews.AddOrUpdate(preview, new Preview(GetPreviewSource(source)));
    
    internal static CardModel GetPreviewSource(CardModel card) =>
        Previews.TryGetValue(card, out var preview) ? preview.Source : card;
    
    /// <summary>
    /// 将不在手牌或消耗牌堆中的战斗卡牌放回手牌，兼容自定义牌堆。
    /// 只有卡牌实际进入手牌才算成功；手牌已满等导致的移动失败不算召回成功。
    /// </summary>
    public static async Task<bool> Recall(CardModel card)
    {
        if (card.CombatState == null || card.Pile?.Type is null or PileType.Hand or PileType.Exhaust) return false;
        var result = await CardPileCmd.Add(card, PileType.Hand);
        return result.success && card.Pile?.Type == PileType.Hand;
    }
    
    /// <summary>
    /// 从抽牌堆和弃牌堆的并集中，随机抽取一张与指定卡牌“同名”的卡牌放入手牌。
    /// </summary>
    /// <param name="card">打出的参照卡牌</param>
    /// <returns>添加结果，如果没有找到有效卡牌则返回 null</returns>
    public static async Task<CardPileAddResult?> DrawSameCardAsync(CardModel card)
    {
        // 1. 优雅的并集获取：利用官方提供的 CardPile.GetCards 静态方法，直接传入多个 PileType 即可获取它们的并集卡牌流。
        var availableCards = CardPile.GetCards(card.Owner, PileType.Draw, PileType.Discard);
        
        // 2. 筛选同类卡牌：判断 ModelId 是否一致。注意：用 c != card 防止极小概率下把自己给抽上来。
        var validTargets = availableCards.Where(c => c.Id == card.Id && c != card).ToList();
        if (validTargets.Count == 0)
            return null; // 牌库和弃牌堆里都没有这张牌的其他复制品了
        
        // 3. 核心：使用玩家 RunState 中官方提供的随机数生成器 (Rng) 的内置扩展方法 NextItem 随机挑出一张，
        // 在 STS2 中，战斗内卡牌相关的随机选择（如发现、从特定池子抓牌）通常使用 CombatCardSelection 随机种子。
        var selectedCard = card.Owner.RunState.Rng.CombatCardSelection.NextItem(validTargets)!;
        
        // 4. 调用官方卡牌移动指令，将选中的卡牌加入手牌。这会自动处理 UI 动画、卡牌位置插值以及触发相关的钩子（Hook）。
        return await CardPileCmd.Add(selectedCard, PileType.Hand);
    }
    
    public static async Task<CardPileAddResult?> FormationCmd(CardModel card)
    {
        var availableCards = CardPile.GetCards(card.Owner, PileType.Draw, PileType.Discard);
        
        // 【核心修改】：在过滤条件中加入对基础费用的判断，彻底杜绝 0 费牌的无限循环
        var validTargets = availableCards.Where(c => 
            c.Id == card.Id && c != card && 
            (c.EnergyCost.Canonical > 0 || c.EnergyCost.CostsX)).ToList(); 
        
        if (validTargets.Count == 0) return null;
        
        var selectedCard = card.Owner.RunState.Rng.CombatCardSelection.NextItem(validTargets)!;
        return await CardPileCmd.Add(selectedCard, PileType.Hand);
    }
}