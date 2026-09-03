using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Others;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// 战斗结束时，为每名玩家将最后进入消耗牌堆、且与 Deck 直接关联的舰载机变化为执念。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class AircraftDeckTransformationPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState combatState, ref Task __result)
    {
        // 用组合后的 Task 替换原返回值，确保变化仍处于原版战斗结束流程的 await 链中，
        // 并发生在 Player.AfterCombatEnd 清空战斗牌堆以及 SaveRun 存档之前。
        __result = TransformExhaustedAircraftAfterHooks(__result, combatState);
    }
    
    private static async Task TransformExhaustedAircraftAfterHooks(Task originalTask, ICombatState combatState)
    {
        await originalTask;
        // CardCmd.Exhaust 总是把牌添加到消耗牌堆底部，因此列表顺序即进入顺序。
        // 先过滤无 Deck 本体的战斗内复制、克隆和生成牌，再取最后一张，避免临时牌阻挡变化。
        var deckCardsToTransform = combatState.Players
            .Select(player => player.PlayerCombatState?.ExhaustPile.Cards
                .LastOrDefault(IsDeckBackedAircraft)?.DeckVersion)
            .OfType<CardModel>()
            .ToList();
        // TransformTo 在原牌的 Run 作用域创建全新的 Resolve，不会复制来源牌的升级或附魔。
        // Resolve 无升级且不能被附魔，因此结果始终是干净的基础执念。
        foreach (var deckCard in deckCardsToTransform) await CardCmd.TransformTo<Resolve>(deckCard);
    }
    
    // DeckVersion 是 PopulateCombatState 为 Deck 牌的战斗版本建立的精确反向引用。
    // CreateClone / CreateDupe 与战斗内生成牌的 DeckVersion 均为空；战斗外 Dupe 则由 IsDupe 排除。
    // 不检查 IsClone：RunState.CloneCard 产生的永久克隆是 Deck 中的独立牌，理应变化该克隆本身。
    private static bool IsDeckBackedAircraft(CardModel combatCard) => 
        combatCard.IsCarrierAircraft() && 
        combatCard is { IsDupe: false, DeckVersion: { IsDupe: false, Pile.Type: PileType.Deck, IsTransformable: true } };
}