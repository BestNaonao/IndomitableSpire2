using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public class CustomCardPileCmd
{
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