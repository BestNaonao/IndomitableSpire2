using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards;

namespace TashkentSpire2.TashkentSpire2Code.Commands;

public static class Loadcmd
{
    public static async Task Execute(PlayerChoiceContext choiceContext, CardModel? targetCard, int amount)
    {
        if (amount <= 0 || targetCard == null) return;
        
        if (targetCard is IAmmunitionCard ammuCard)
        {
            int max = targetCard.DynamicVars.ContainsKey("TashkentSpire2-Ammu-Max") 
                ? targetCard.DynamicVars["TashkentSpire2-Ammu-Max"].IntValue 
                : 99;
            
            int newValue = Math.Min(ammuCard.CurrentAmmu + amount, max);
            ammuCard.UpdateAmmuGlobal(newValue);
        }
        
        await Task.CompletedTask;
    }
}