using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class CustomCardSelectCmd
{
    /// <summary>
    /// 从指定的战斗内牌堆中，选择卡牌以进行临时附魔，调出带有附魔提示框的专属界面进行选择。
    /// 支持单牌堆或多个牌堆的并集（例如同时展示手牌和抽牌堆）。兼容多人联机模式。
    /// </summary>
    public static async Task<IEnumerable<CardModel>> FromCombatForEnchantment(
        PlayerChoiceContext context,
        Player player,
        EnchantmentModel enchantment,
        int amount,
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? additionalFilter = null,
        params PileType[] piles)
    {
        // 1. 语义防呆：战斗内的临时附魔绝对不能操作卡组 (Deck)，否则会变成永久修改！
        if (piles.Any(p => !p.IsCombatPile()))
            throw new ArgumentException("Combat enchantment selection cannot include PileType.Deck or PileType.None.");
        
        // 2. 收集所有指定牌堆的卡牌并进行初步过滤
        var validCards = CardPile.GetCards(player, piles).Where(c => 
            // 必须能被该附魔目标接纳，并附加用户的自定义过滤条件
            enchantment.CanEnchant(c) && (additionalFilter == null || additionalFilter(c))
        ).ToList();
        
        // 3. 边界处理：如果没有合法的牌，直接返回空集合（原版 FromSimpleGrid 和 FromHand 也自带了这个处理）
        if (validCards.Count == 0) return [];
        
        IReadOnlyList<CardModel> result;
        
        // 4. 处理自动确认逻辑（例如只让选1张，且刚好只有1张合法牌）
        if (!prefs.RequireManualConfirmation && validCards.Count <= prefs.MinSelect)
        {
            result = validCards;
        }
        else
        {
            // --- 多人联机同步核心开始 ---
            var choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
            await context.SignalPlayerChoiceBegun(player, PlayerChoiceOptions.None);
            
            if (LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay)
            {
                // 【本地玩家逻辑】
                if (CardSelectCmd.Selector != null) // 自动化测试环境兼容
                {
                    result = (await CardSelectCmd.Selector.GetSelectedCards(validCards, prefs.MinSelect, prefs.MaxSelect)).ToList();
                }
                else
                {
                    // 确保玩家手牌没在被拖拽
                    NPlayerHand.Instance?.CancelAllCardPlay();
                    // 【核心 UI 替换】：调用原版的牌组附魔专属 UI，但传入我们组装的战斗内卡牌列表！
                    var screen = NDeckEnchantSelectScreen.ShowScreen(validCards, enchantment, amount, prefs);
                    result = (await screen.CardsSelected()).ToList();
                }
                // 【核心同步替换】：绝对不能用 FromMutableDeckCards！直接使用实体 CombatCards 进行同步，更安全！
                RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                    player, 
                    choiceId, 
                    PlayerChoiceResult.FromMutableCombatCards(result)
                );
            }
            else
            {
                // 【远程队友逻辑】：接收本地玩家传来的 Index，还原为卡牌引用
                result = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId)).AsCombatCards().ToList();
            }
            await context.SignalPlayerChoiceEnded();
        }
        MainFile.Logger.Info($"Player {player.NetId} combat-enchanted cards: {string.Join(",", result.Select(c => c.Id.Entry))}");
        return result;
    }
}