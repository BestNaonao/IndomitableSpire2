using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public class ReconCmd
{
    // 创建自定义的本地化提示框（需要在 json 中配置该文本）
    private static LocString DrawPrompt => new("card_selection", "INDOMITABLESPIRE2-RECON_TO_HAND");
    private static LocString DiscardPrompt => new("card_selection", "INDOMITABLESPIRE2-RECON_TO_DISCARD");
    /// <summary>
    /// 执行侦察机制：查看牌库顶部的牌，选择部分放入手牌，选择部分弃置，其余保留。
    /// 也可以在这里加入侦察前后触发的钩子。
    /// </summary>
    public static async Task Execute(PlayerChoiceContext choiceContext, Player player, int amount)
    {
        if (amount <= 0) return;
        
        // 1. 获取牌库顶部的 X 张牌作为“侦察池”
        var cardsToRecon = PileType.Draw.GetPile(player).Cards.Take(amount).ToList();
        if (cardsToRecon.Count == 0) return;
        
        // ================= 阶段 1：选择放入手牌 =================
        // 弹出网格界面供玩家选择
        var cardsToHand = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            cardsToRecon,
            player,
            new CardSelectorPrefs(DrawPrompt, 0, cardsToRecon.Count)
        )).ToList();
        
        // 处理选中的卡牌：加入手牌，并从侦察池中移除
        foreach (var card in cardsToHand)
        {
            // 此处必须使用 CardPileCmd.Add，不能使用 CardPileCmd.Draw。
            await CardPileCmd.Add(card, PileType.Hand);
            cardsToRecon.Remove(card); 
        }
        
        // 如果第一阶段把牌全拿光了，直接结束指令
        if (cardsToRecon.Count == 0) return;
        
        // ================= 阶段 2：选择弃置 =================
        var cardsToDiscard = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            cardsToRecon,
            player,
            new CardSelectorPrefs(DiscardPrompt, 0, cardsToRecon.Count)
        )).ToList();
        
        // 弃置选中的卡牌
        foreach (var card in cardsToDiscard)
            await CardCmd.Discard(choiceContext, card);
        
        // ================= 阶段 3：其余保持不动 =================
        // 凡是没有被放入手牌、也没有被弃置的牌，原版引擎会自动将它们留在抽牌堆的原始位置（顶部），
        // 所以我们不需要编写任何额外代码，机制自然闭环！
    }
}