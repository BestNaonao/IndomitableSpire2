using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Models;

public sealed class CardMotivationCost
{
    private readonly CardModel _card;
    // 修改器栈
    private readonly List<MotivationCostModifier> _localModifiers = [];
    
    public CardMotivationCost(CardModel card)
    {
        _card = card;
    }
    
    // 传入 BaseValue，遍历修改器栈，得出受“免费/降费”影响后的最终实际消耗
    public int GetWithModifiers(int baseCost)
    {
        var val = baseCost;
        foreach (var modifier in _localModifiers)
            val = modifier.Modify(val);
        // 干劲消耗最低为 0
        return Math.Max(0, val);
    }
    
    // 设置为本回合或直到打出前免费（完美对齐 SetToFreeThisTurn）
    public void SetThisTurnOrUntilPlayed(int cost, bool reduceOnly = false)
    {
        _card.AssertMutable(); // 【安全断言】：确保卡牌处于战斗/可修改状态
        _localModifiers.Add(new MotivationCostModifier(
            cost, 
            LocalCostType.Absolute, 
            LocalCostModifierExpiration.EndOfTurn | LocalCostModifierExpiration.WhenPlayed, 
            reduceOnly));
    }
    
    public void SetThisTurn(int cost, bool reduceOnly = false)
    {
        _card.AssertMutable();
        _localModifiers.Add(new MotivationCostModifier(
            cost, 
            LocalCostType.Absolute, 
            LocalCostModifierExpiration.EndOfTurn, 
            reduceOnly));
    }
    
    // 设置为本场战斗免费（完美对齐 SetThisCombat）
    public void SetThisCombat(int cost, bool reduceOnly = false)
    {
        _card.AssertMutable();
        _localModifiers.Add(new MotivationCostModifier(
            cost, 
            LocalCostType.Absolute, 
            LocalCostModifierExpiration.EndOfCombat, 
            reduceOnly));
    }
    
    // 回合结束时的清理逻辑
    public bool EndOfTurnCleanup()
    {
        _card.AssertMutable();
        return _localModifiers.RemoveAll(m => m.Expiration.HasFlag(LocalCostModifierExpiration.EndOfTurn)) > 0;
    }
    
    // 打出卡牌后的清理逻辑
    public bool AfterCardPlayedCleanup()
    {
        _card.AssertMutable();
        return _localModifiers.RemoveAll(m => m.Expiration.HasFlag(LocalCostModifierExpiration.WhenPlayed)) > 0;
    }
}