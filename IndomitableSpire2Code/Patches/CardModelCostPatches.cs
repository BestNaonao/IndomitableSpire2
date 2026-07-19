using System.Reflection;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(CardModel))]
public static class CardModelCostPatches
{
    // 1. 拦截“本回合免费”
    [HarmonyPatch(nameof(CardModel.SetToFreeThisTurn))]
    [HarmonyPostfix]
    public static void SetToFreeThisTurn_Postfix(CardModel __instance)
    {
        // 只要原版给卡牌设置了本回合免费，我们也给干劲设置本回合免费
        __instance.SetMotivationFreeThisTurn();
    }
    
    // 2. 拦截“本场战斗免费”
    [HarmonyPatch(nameof(CardModel.SetToFreeThisCombat))]
    [HarmonyPostfix]
    public static void SetToFreeThisCombat_Postfix(CardModel __instance)
    {
        // 调用你在扩展类中补充的对应方法
        __instance.SetMotivationFreeThisCombat(); 
    }
    
    // 3. 拦截“回合结束清理”
    [HarmonyPatch(nameof(CardModel.EndOfTurnCleanup))]
    [HarmonyPostfix]
    public static void EndOfTurnCleanup_Postfix(CardModel __instance)
    {
        // 触发干劲管理器的回合清理逻辑（清除 EndOfTurn 类型的修改器）
        // 如果确实有修改器被清理了（费用恢复原价），则强制刷新 UI
        if (__instance.GetMotivationCost().EndOfTurnCleanup())
        {
            __instance.InvokeEnergyCostChanged();
        }
    }
    
    // 4. 拦截“是否花费能量或星星”的判定，将干劲消耗也纳入其中
    [HarmonyPatch(nameof(CardModel.CostsEnergyOrStars))]
    [HarmonyPostfix]
    public static void CostsEnergyOrStars_Postfix(CardModel __instance, bool includeGlobalModifiers, ref bool __result)
    {
        // 1. 如果原版已经判定为 true（确实花费能量或星星），不需要任何修改，直接放行
        if (__result) return;
        
        // 2. 如果原版判定为 false，检查这张卡是否有“干劲消耗”变量
        if (__instance.ConsumesMotivation())
        {
            // 获取基础干劲消耗与实际的干劲消耗
            var baseMotivationCost = __instance.DynamicVars.MotivationConsume().IntValue;
            if (CheckMotivationCost(__instance, includeGlobalModifiers, baseMotivationCost))
            {
                __result = true;
            }
        }
        
        // 3. 进一步检查这张卡是否有“干劲需求”变量
        if (__instance.RequiresMotivation())
        {
            var baseMotivationCost = __instance.DynamicVars.MotivationRequire().IntValue;
            if (CheckMotivationCost(__instance, includeGlobalModifiers, baseMotivationCost))
            {
                __result = true;
            }
        }
    }
    
    public static bool CheckMotivationCost(CardModel card, bool includeGlobalModifiers, int baseCost)
    {
        var actualCost = card.GetActualMotivationCost(baseCost);
        switch (includeGlobalModifiers)
        {
            case true when actualCost > 0:
            case false when baseCost > 0 || actualCost > 0:
                return true;
        }
        return false;
    }
}

[HarmonyPatch]
public static class CardCostCleanupPatches
{
    // 获取 CardEnergyCost 内部的私有字段 _card
    private static readonly FieldInfo CardFieldInfo = AccessTools.Field(typeof(CardEnergyCost), "_card");
    
    [HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.AfterCardPlayedCleanup))]
    [HarmonyPostfix]
    public static void AfterCardPlayedCleanup_Postfix(CardEnergyCost __instance)
    {
        // 1. 通过反射获取到这个能量管理器属于哪张卡牌
        if (CardFieldInfo.GetValue(__instance) is CardModel card)
        {
            // 2. 触发干劲管理器的打出后清理逻辑（清除 WhenPlayed 类型的修改器）
            if (card.GetMotivationCost().AfterCardPlayedCleanup())
            {
                // 3. 如果费用恢复了原价，强制刷新 UI
                card.InvokeEnergyCostChanged();
            }
        }
    }
}