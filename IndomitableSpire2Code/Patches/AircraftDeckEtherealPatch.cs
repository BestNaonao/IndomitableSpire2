using BaseLib.Utils;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

public static class AircraftDeckEtherealSave
{
    /// <summary>
    /// 记录卡牌是否被舰载机战后机制永久赋予过虚无。
    /// CardModel 的 LocalKeywords 不会进入 SerializableCard，必须额外保存这个来源标记。
    /// </summary>
    public static readonly SavedSpireField<CardModel, bool> AircraftGrantedEtherealField = 
        new(() => false, "IndomitableSpire2_AircraftGrantedEthereal");
    
    /// <summary>
    /// 为牌组卡牌永久添加由舰载机战后机制赋予的虚无。
    /// </summary>
    public static void ApplyAircraftGrantedEthereal(this CardModel card)
    {
        AircraftGrantedEtherealField.Set(card, true);
        CardCmd.ApplyKeyword(card, CardKeyword.Ethereal);
    }
    
    /// <summary>
    /// 卡牌从存档或联机数据重建后，根据持久化来源恢复虚无关键词。
    /// </summary>
    public static void RestoreAircraftGrantedEthereal(this CardModel card)
    {
        if (!AircraftGrantedEtherealField.Get(card) || 
            card.GetKeywordsWithSources(KeywordSources.Local).Contains(CardKeyword.Ethereal))
            return;
        // 此时卡牌尚未进入牌堆，也没有需要刷新的 NCard，直接修改模型即可。
        card.AddKeyword(CardKeyword.Ethereal);
    }
    
    static AircraftDeckEtherealSave()
    {
        // 牌组卡牌被多利之镜等局外效果克隆时，持久化来源也必须随之复制。
        AircraftGrantedEtherealField.CopyOnClone();
    }
}

/// <summary>
/// 战斗结束时，为消耗牌堆中所有舰载机对应的牌组版本添加虚无。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class AircraftDeckEtherealPatch
{
    [HarmonyPostfix]
    public static void Postfix(ICombatState combatState, ref Task __result)
    {
        // 保持在原版战斗结束流程的 await 链中，并在战斗牌堆清空和战后存档之前修改 Deck。
        __result = ApplyEtherealAfterHooks(__result, combatState);
    }
    
    private static async Task ApplyEtherealAfterHooks(Task originalTask, ICombatState combatState)
    {
        await originalTask;
        var deckCards = combatState.Players
            .SelectMany(player => player.PlayerCombatState?.ExhaustPile.Cards ?? [])
            .Where(card => card.IsCarrierAircraft())
            .Select(card => card.DeckVersion)
            .OfType<CardModel>()
            .Where(card => card.Pile?.Type == PileType.Deck)
            .Distinct()
            .ToList();
        foreach (var deckCard in deckCards) deckCard.ApplyAircraftGrantedEthereal();
    }
}

/// <summary>
/// SerializableCard 不保存 CardModel.LocalKeywords；从存档或其他联机玩家的数据重建卡牌后，
/// 使用 SavedSpireField 中的来源标记恢复舰载机机制赋予的虚无。
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable))]
public static class RestoreAircraftGrantedEtherealPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __result)
    {
        __result.RestoreAircraftGrantedEthereal();
    }
}
