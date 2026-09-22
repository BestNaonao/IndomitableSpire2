using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class CustomCardSelectCmd
{
    /// <summary>
    /// 从调用方提供的战斗卡牌中选择可附魔的牌，使用原版附魔选择界面。
    /// 保留候选牌的输入顺序，兼容多人联机模式。
    /// </summary>
    public static async Task<IEnumerable<CardModel>> FromCombatForEnchantment(
        PlayerChoiceContext context,
        Player player,
        IReadOnlyList<CardModel> cards,
        EnchantmentModel enchantment,
        int amount,
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? additionalFilter = null)
    {
        // 临时附魔仅接受战斗牌，避免误改永久卡组。
        if (cards.Any(card => card.Pile?.IsCombatPile != true))
            throw new ArgumentException("Combat enchantment selection requires cards in combat piles.", nameof(cards));
        
        // 只过滤附魔资格，不收集牌堆或改变展示顺序。
        var validCards = cards.Where(c =>
            // 必须能被该附魔目标接纳，并附加用户的自定义过滤条件
            enchantment.CanEnchant(c) && (additionalFilter == null || additionalFilter(c))
        ).ToList();
        
        return await SelectCombatCards(context, player, validCards, prefs, displayCards =>
            NDeckEnchantSelectScreen.ShowScreen(displayCards, enchantment, amount, prefs));
    }
    
    /// <summary>
    /// 使用普通战斗选牌界面，并在右下角显示附魔信息。
    /// 候选牌和顺序由调用方提供，因此也能包含只可升级、不可附魔的牌。
    /// </summary>
    public static Task<IEnumerable<CardModel>> FromCombatWithEnchantmentInfo(
        PlayerChoiceContext context,
        Player player,
        IReadOnlyList<CardModel> cards,
        EnchantmentModel enchantment,
        CardSelectorPrefs prefs) =>
        SelectCombatCards(context, player, cards, prefs, displayCards =>
        {
            // 已由调用方排好顺序，不再通过原版界面的 Comparison 重排。
            var screen = NSimpleCardSelectScreen.Create(displayCards, prefs with { Comparison = null });
            screen.Ready += () => AddEnchantmentInfo(screen, enchantment);
            NOverlayStack.Instance!.Push(screen);
            return screen;
        });
    
    private static void AddEnchantmentInfo(NSimpleCardSelectScreen screen, EnchantmentModel enchantment)
    {
        // 只取原版的说明面板，保留它的右下角定位、字体、图标和自动布局。
        // 不使用附魔确认预览：它会把不能附魔的牌也预览为已附魔。
        var template = PreloadManager.Cache.GetScene(
            SceneHelper.GetScenePath("screens/card_selection/deck_enchant_select_screen")).Instantiate<Control>();
        var panel = template.GetNode<Control>("%EnchantmentDescriptionContainer");
        var title = template.GetNode<MegaLabel>("%EnchantmentTitle");
        var description = template.GetNode<MegaRichTextLabel>("%EnchantmentDescription");
        var icon = template.GetNode<TextureRect>("%EnchantmentIcon");
        template.RemoveChild(panel);
        template.Free();
        screen.AddChild(panel);
        
        var display = (EnchantmentModel)enchantment.ClonePreservingMutability();
        display.RecalculateValues();
        title.SetTextAutoSize(display.Title.GetFormattedText());
        description.Text = display.DynamicDescription.GetFormattedText();
        icon.Texture = display.Icon;
        screen.GetNode<NPeekButton>("%PeekButton").AddTargets(panel);
    }
    
    private static async Task<IEnumerable<CardModel>> SelectCombatCards(
        PlayerChoiceContext context,
        Player player,
        IReadOnlyList<CardModel> validCards,
        CardSelectorPrefs prefs,
        Func<IReadOnlyList<CardModel>, NCardGridSelectionScreen> showScreen)
    {
        // 没有候选牌时不打开选择界面。
        if (validCards.Count == 0) return [];
        
        // 固定此次选择的候选牌快照，保持调用方提供的顺序。
        validCards = validCards.ToArray();
        
        // 与原版 FromSimpleGrid 一样，自动选牌不启动玩家选择和联网界面。
        if (CardSelectCmd.Selector != null)
            return await CardSelectCmd.Selector.GetSelectedCards(validCards, prefs.MinSelect, prefs.MaxSelect);
        
        IReadOnlyList<CardModel> result;
        
        // 处理自动确认逻辑（例如只让选1张，且刚好只有1张合法牌）。
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
                if (CardSelectCmd.LocalSelector != null)
                {
                    result = (await CardSelectCmd.LocalSelector.GetSelectedCards(validCards, prefs.MinSelect, prefs.MaxSelect)).ToList();
                }
                else
                {
                    // 确保玩家手牌没在被拖拽
                    NPlayerHand.Instance?.CancelAllCardPlay();
                    var screen = showScreen(validCards);
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
        MainFile.Logger.Info($"Player {player.NetId} selected combat cards: {string.Join(",", result.Select(c => c.Id.Entry))}");
        return result;
    }
}