using MegaCrit.Sts2.Core.Entities.Cards;

namespace IndomitableSpire2.IndomitableSpire2Code.Models;

public class MotivationCostModifier
{
    public int Amount { get; set; }
    public LocalCostType Type { get; }
    public LocalCostModifierExpiration Expiration { get; }
    public bool IsReduceOnly { get; }
    
    public MotivationCostModifier(int amount, LocalCostType type, LocalCostModifierExpiration expiration, bool reduceOnly)
    {
        Amount = amount;
        Type = type;
        Expiration = expiration;
        IsReduceOnly = reduceOnly;
    }
    
    // 执行修改计算
    public int Modify(int currentCost) => Type switch 
    { 
        LocalCostType.Absolute => IsReduceOnly ? Math.Min(currentCost, Amount) : Amount, 
        LocalCostType.Relative => IsReduceOnly ? Math.Min(currentCost, currentCost + Amount) : currentCost + Amount, 
        _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, null) 
    };
    
    public MotivationCostModifier Clone()
    {
        return new MotivationCostModifier(Amount, Type, Expiration, IsReduceOnly);
    }
}