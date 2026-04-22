using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace TashkentSpire2.TashkentSpire2Code.Commands;

public static class SaturationBombingcmd
{
    public static async Task Execute(PlayerChoiceContext choiceContext, Player? player, CardModel? targetCard)
    {
        if (player == null || targetCard == null)
            return;
        
        var selected = (await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            player,
            new CardSelectorPrefs(new LocString("cards", targetCard.Id.Entry + ".selectionScreenPrompt"), 0, 999),
            null,
            targetCard)).ToList();

        if (selected.Count == 0)
            return;
        
        await CardCmd.Discard(choiceContext, selected);
        
        int amount = selected.Count;

        await Loadcmd.Execute(choiceContext, targetCard, amount);
    }
}