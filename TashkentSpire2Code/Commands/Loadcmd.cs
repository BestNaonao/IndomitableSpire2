using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using TashkentSpire2.TashkentSpire2Code.Cards;
using TashkentSpire2.TashkentSpire2Code.Powers;

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
        
        int vigorAmount = (int)(targetCard.Owner?.Creature.GetPower<BarrelModificationPower>()?.Amount ?? 0m);
        if (vigorAmount > 0 && targetCard.Owner?.Creature != null)
        {
            await PowerCmd.Apply<VigorPower>(targetCard.Owner.Creature, (decimal)vigorAmount, targetCard.Owner.Creature, null);
        }
        
        await Task.CompletedTask;
    }
}