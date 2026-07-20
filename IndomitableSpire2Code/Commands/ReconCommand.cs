using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public class ReconCommand
{
    // 创建自定义的本地化提示框（需要在 json 中配置该文本）
    private static LocString DrawPrompt => new("card_selection", "INDOMITABLESPIRE2-RECON_TO_HAND");
    private static LocString DiscardPrompt => new("card_selection", "INDOMITABLESPIRE2-RECON_TO_DISCARD");
    
    private readonly Player _player;
    private readonly int _amount;
    
    // 存放指令执行完毕后的结果
    public IReadOnlyList<CardModel> CardsToHand { get; private set; } = [];
    public IReadOnlyList<CardModel> CardsToDiscard { get; private set; } = [];
    public IReadOnlyList<CardModel> CardsKept { get; private set; } = [];
    
    // 隐藏构造函数，强制使用静态工厂方法创建
    private ReconCommand(Player player, int amount)
    {
        _player = player;
        _amount = amount;
    }
    
    /// <summary>
    /// 静态入口：构建一个侦察指令
    /// </summary>
    public static ReconCommand Recon(Player player, int amount) => new(player, amount);
    
    /// <summary>
    /// 核心执行方法，返回自身以便链式读取结果。
    /// 查看牌库顶部的牌，选择部分放入手牌，选择部分弃置，其余保留。
    /// </summary>
    public async Task<ReconCommand> Execute(PlayerChoiceContext choiceContext)
    {
        if (_amount <= 0) return this;
        
        // 获取牌库顶部的 X 张牌作为“侦察池”
        var cardsToRecon = PileType.Draw.GetPile(_player).Cards.Take(_amount).ToList();
        if (cardsToRecon.Count == 0) return this;
        
        // 阶段 1：选择放入手牌
        var handSelection = await CardSelectCmd.FromSimpleGrid(
            choiceContext, cardsToRecon, _player, new CardSelectorPrefs(DrawPrompt, 0, cardsToRecon.Count));
        var toHandList = handSelection.ToList();
        foreach (var card in toHandList)
        {
            // 此处必须使用 CardPileCmd.Add，不能使用 CardPileCmd.Draw。
            await CardPileCmd.Add(card, PileType.Hand);
            cardsToRecon.Remove(card);
        }
        CardsToHand = toHandList;
        
        if (cardsToRecon.Count == 0) return this;
        
        // 阶段 2：选择弃置
        var discardSelection = await CardSelectCmd.FromSimpleGrid(
            choiceContext, cardsToRecon, _player, new CardSelectorPrefs(DiscardPrompt, 0, cardsToRecon.Count));
        var toDiscardList = discardSelection.ToList();
        foreach (var card in toDiscardList)
        {
            await CardCmd.Discard(choiceContext, card);
            cardsToRecon.Remove(card);
        }
        CardsToDiscard = toDiscardList;
        
        // 阶段 3：记录剩下的牌（保持在牌库顶不动）
        CardsKept = cardsToRecon.ToList();
        
        // 阶段 4：之后我们可以在这里触发我们的自定义钩子。
        // await CustomHook.AfterRecon(CardModel card, Player player, int amount);
        
        return this;
    }
}