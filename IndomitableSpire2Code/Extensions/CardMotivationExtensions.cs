using BaseLib.Utils;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Models;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class CardMotivationExtensions
{
    // 利用 SpireField 无侵入式挂载管理器。注意：初始化时先传 null，在 Get 时动态注入卡牌引用。
    public static readonly SpireField<CardModel, CardMotivationCost> MotivationCostField = new(() => null!);
    
    /// <summary>
    /// 获取该卡牌的干劲管理器（懒加载）
    /// </summary>
    public static CardMotivationCost GetMotivationCost(this CardModel card)
    {
        var cost = MotivationCostField.Get(card);
        if (cost != null) return cost;
        cost = new CardMotivationCost(card);
        MotivationCostField.Set(card, cost);
        return cost;
    }
    
    // 获取该卡牌的真实干劲需求或消耗
    public static int GetActualMotivationCost(this CardModel card, int amount) => 
        card.GetMotivationCost().GetWithModifiers(amount);
    
    // 将该卡牌的干劲消耗/需求设置为本回合免费
    public static void SetMotivationFreeThisTurn(this CardModel card)
    {
        card.GetMotivationCost().SetThisTurnOrUntilPlayed(0);
        // card.InvokeEnergyCostChanged(); // 强制踢引擎一脚，立刻刷新手牌UI
    }
    
    // 将该卡牌的干劲消耗/需求设置为本回合完全免费
    public static void SetMotivationFreeThisEntireTurn(this CardModel card)
    {
        card.GetMotivationCost().SetThisTurn(0);
    }
    
    // 将该卡牌的干劲消耗/需求设置为本场战斗免费
    public static void SetMotivationFreeThisCombat(this CardModel card)
    {
        card.GetMotivationCost().SetThisCombat(0);
        // card.InvokeEnergyCostChanged(); // 强制踢引擎一脚，立刻刷新手牌UI
    }
    
    // ====== 战斗逻辑扩展 ======
    
    /// <summary>
    /// 一键扣除干劲费用的标准扩展方法。自动计算打折/免费后的最终数值！
    /// </summary>
    public static async Task SpendMotivationCost(this CardModel card)
    {
        if (!card.DynamicVars.ContainsKey(MotivationConsumeVar.DefaultName)) return;
        
        // 读取修改后的真实花费
        var actualCost = card.GetActualMotivationCost(card.DynamicVars.MotivationConsume().IntValue);
        if (actualCost > 0)
        {
            await PowerCmd.Apply<MotivationPower>(
                target: card.Owner.Creature,
                amount: -actualCost, // 扣减干劲
                applier: card.Owner.Creature,
                cardSource: card
            );
        }
    }
    
    // 纯粹的底层判断：是否有 X 点干劲
    public static bool HasEnoughMotivation(this CardModel card, int amount) => 
        card.CombatState != null && 
        card.Owner.Creature.GetPower<MotivationPower>() is PowerModel power && 
        power.DisplayAmount >= amount;
    
    // 高阶判断：是否满足“需求”变量的数值
    public static bool MeetsMotivationRequirement(this CardModel card) => 
        !card.DynamicVars.ContainsKey(MotivationRequireVar.DefaultName) || 
        card.HasEnoughMotivation(card.GetActualMotivationCost(card.DynamicVars.MotivationRequire().IntValue));
    
    // 高阶判断：是否足够支付“消耗”变量的数值
    public static bool CanAffordMotivationCost(this CardModel card) => 
        !card.DynamicVars.ContainsKey(MotivationConsumeVar.DefaultName) ||
        card.HasEnoughMotivation(card.GetActualMotivationCost(card.DynamicVars.MotivationConsume().IntValue));
    
    // 终极组合验证：只要卡牌注册了这两种变量，就自动双重验证！
    // 这是最强大的框架方法，以后写卡牌可以直接无脑调用这个
    public static bool ValidateMotivationConditions(this CardModel card) =>
        card.MeetsMotivationRequirement() && card.CanAffordMotivationCost();
}