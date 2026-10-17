using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

// 单张重载会调用批量重载，只在后者广播，以免同一次升级触发两次。
[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Upgrade), [typeof(IEnumerable<CardModel>), typeof(CardPreviewStyle)])]
public static class CardCmdUpgradePatch
{
    [HarmonyPrefix]
    public static void Prefix(ref IEnumerable<CardModel> cards, out Dictionary<CardModel, int> __state)
    {
        // 固定同一批实例，兼容只能遍历一次或每次枚举会创建新牌的输入。
        cards = cards.ToArray();
        __state = cards.Distinct().ToDictionary(card => card, card => card.CurrentUpgradeLevel);
    }
    
    [HarmonyPostfix]
    public static void Postfix(Dictionary<CardModel, int> __state)
    {
        // 广播前固定结果，监听者引起的后续升级由其自己的命令通知，不计入本次差值。
        var upgrades = __state.Select(entry 
                => (Card: entry.Key, Levels: entry.Key.CurrentUpgradeLevel - entry.Value))
            .Where(entry => entry.Levels > 0).ToArray();
        foreach (var (card, levels) in upgrades)
        {
            if (card.RunState is not {} runState) continue;
            for (var i = 0; i < levels; i++)
                CustomHook.AfterCardUpgraded(runState, card.Owner.Creature.CombatState, card);
        }
    }
}