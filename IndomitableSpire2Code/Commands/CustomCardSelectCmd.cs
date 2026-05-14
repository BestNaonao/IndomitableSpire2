using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class CustomCardSelectCmd
{
    /// <summary>
    /// 从指定的战斗内牌堆中，选择卡牌以进行临时附魔。
    /// 支持单牌堆或多个牌堆的并集（例如同时展示手牌和抽牌堆）。
    /// </summary>
    public static async Task<IEnumerable<CardModel>> FromCombatForEnchantment(
        PlayerChoiceContext context,
        Player player,
        EnchantmentModel enchantment,
        int amount, // 保留该参数以对齐原版方法签名，未来如果我们要手写定制化的附魔预览UI也会用到它
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? additionalFilter = null,
        params PileType[] piles)
    {
        // 1. 语义防呆：战斗内的临时附魔绝对不能操作卡组 (Deck)，否则会变成永久修改！
        if (piles.Contains(PileType.Deck) || piles.Contains(PileType.None))
            throw new ArgumentException("Combat enchantment selection cannot include PileType.Deck or PileType.None.");
        
        // 2. 收集所有指定牌堆的卡牌并进行初步过滤
        var validCards = CardPile.GetCards(player, piles).Where(c => 
            // 必须能被该附魔目标接纳（比如攻击牌专属附魔不能选技能牌），并附加用户的自定义过滤条件
            enchantment.CanEnchant(c) && (additionalFilter == null || additionalFilter(c))
        ).ToList();
        
        // 3. 边界处理：如果没有合法的牌，直接返回空集合（原版 FromSimpleGrid 和 FromHand 也自带了这个处理）
        if (validCards.Count == 0) return [];
        
        // 4. 智能 UI 路由
        // 如果玩家只指定了手牌，调用 FromHand 提供更舒适的底层扇形点选体验
        if (piles is [PileType.Hand])
            return await CardSelectCmd.FromHand(
                context, 
                player, 
                prefs, 
                c => validCards.Contains(c), // 再次应用过滤结果
                enchantment // 将附魔本身作为特效溯源源头
            );
        
        // 如果涉及多个牌堆（如手牌+抽牌堆的并集），或者指定的仅仅是弃牌堆/消耗堆，则使用网格选择 UI
        return await CardSelectCmd.FromSimpleGrid(
            context,
            validCards,
            player,
            prefs
        );
    }
}