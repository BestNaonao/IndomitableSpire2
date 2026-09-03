using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// 战斗结束时，为每名玩家将最后进入消耗牌堆、且与卡组直接关联的舰载机永久移出卡组。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class AircraftDeckRemovalPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState combatState, ref Task __result)
    {
        // 用组合后的 Task 替换原返回值，确保移除操作仍处于原版战斗结束流程的 await 链中，
        // 并发生在 Player.AfterCombatEnd 清空战斗牌堆以及 SaveRun 存档之前。
        __result = RemoveExhaustedAircraftAfterHooks(__result, combatState);
    }
    
    private static async Task RemoveExhaustedAircraftAfterHooks(Task originalTask, ICombatState combatState)
    {
        await originalTask;
        // CardCmd.Exhaust 总是把牌添加到消耗牌堆底部，因此列表顺序即进入顺序。
        // 先过滤无卡组本体的战斗内复制/克隆/生成牌，再取最后一张，避免临时牌阻挡删卡。
        var deckCardsToRemove = combatState.Players
            .Select(player => player.PlayerCombatState?.ExhaustPile.Cards
                .LastOrDefault(IsRemovableDeckAircraft)?.DeckVersion)
            .OfType<CardModel>()
            .ToList();
        // 此钩子会在所有联机端确定性执行。批量命令负责触发原版移除钩子、记录本房间历史，并且只在卡牌所属玩家的本地客户端显示移除预览。
        if (deckCardsToRemove.Count != 0) await CardPileCmd.RemoveFromDeck(deckCardsToRemove, showPreview: true);
    }
    
    // DeckVersion 是 PopulateCombatState 为卡组牌的战斗版本建立的精确反向引用。
    // CreateClone / CreateDupe 与战斗内生成牌的 DeckVersion 均为空；战斗外 Dupe 则由 IsDupe 排除。
    // 不检查 IsClone：RunState.CloneCard 产生的永久克隆是卡组中的独立牌，理应移除该克隆本身。
    private static bool IsRemovableDeckAircraft(CardModel combatCard) => 
        combatCard.IsCarrierAircraft() && 
        combatCard is { IsDupe: false, DeckVersion: { IsDupe: false, Pile.Type: PileType.Deck } };
}