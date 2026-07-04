using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TashkentSpire2.TashkentSpire2Code.Cards;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Commands;

public static class Loadcmd
{
    public static async Task Execute(PlayerChoiceContext? choiceContext, CardModel? targetCard, int amount)
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
        
        int blockAmount = (int)(targetCard.Owner?.Creature.GetPower<BarrelModificationPower>()?.Amount ?? 0m);
        if (blockAmount > 0 && targetCard.Owner?.Creature != null)
        {
            await CreatureCmd.GainBlock(targetCard.Owner.Creature, blockAmount, ValueProp.Unpowered, null);
        }
        
        await Task.CompletedTask;
    }
}