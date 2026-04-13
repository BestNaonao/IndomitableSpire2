using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards.Basics;

namespace TashkentSpire2.TashkentSpire2Code.Commands;

public static class Loadcmd
{
    public static async Task Execute(PlayerChoiceContext choiceContext, CardModel? targetCard, int amount)
    {
        if (amount <= 0 || targetCard == null)
            return;
        
        var dyn = targetCard?.DynamicVars;

        if (dyn == null ||
            !dyn.ContainsKey("TashkentSpire2-Ammu") ||
            !dyn.ContainsKey("TashkentSpire2-Ammu-Max"))
            return;

        int current = dyn["TashkentSpire2-Ammu"].IntValue;
        int max = dyn["TashkentSpire2-Ammu-Max"].IntValue;

        if (max <= 0)
            return;

        int newValue = Math.Min(current + amount, max);
        
        if (targetCard is LoadShot loadShot)
            loadShot.CurrentAmmu = newValue;
        else
            dyn["TashkentSpire2-Ammu"].BaseValue = newValue;

        await Task.CompletedTask;
    }
}