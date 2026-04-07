using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Watcher.Code.Commands;

public static class BestofBestCmd
{
    public static async Task Execute(PlayerChoiceContext choiceContext, Player player, int amount)
    {
        if (amount <= 0) return;

        var drawPile = PileType.Draw.GetPile(player);
        var cardsToScry = drawPile.Cards.Take(amount).ToList();


        if (!cardsToScry.Any()) return;
        var prefs = new CardSelectorPrefs(
            CardSelectorPrefs.DiscardSelectionPrompt,
            0,
            cardsToScry.Count
        );

        var cardsToDiscard = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            cardsToScry,
            player,
            prefs
        );
        
        foreach (var card in cardsToDiscard)
        {
            CardCmd.Upgrade(card);
            await CardPileCmd.Add(card, PileType.Discard);
        }

        await CardPileCmd.Draw(choiceContext, amount-cardsToDiscard.Count(), player);
    }
}