using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class CardExtensions
{
    public static bool HasEnoughMotivation(this CardModel card) =>
        card.HasEnoughMotivation(card.DynamicVars.MotivationConsume().IntValue);
    
    public static bool HasEnoughMotivation(this CardModel card, int amount) =>
        card.CombatState != null && 
        card.Owner.Creature.GetPower<MotivationPower>() is PowerModel power && 
        power.DisplayAmount >= amount;
}