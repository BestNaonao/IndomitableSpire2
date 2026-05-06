using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;


[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCardPlayed))]
public static class FormationPatch
{
    // 🔹 提取异步逻辑到静态方法，避免闭包分配
    private static async Task RunFormationAfterOriginal(Task originalTask, CardModel card)
    {
        await originalTask; // 等待原版所有钩子执行完毕
        await CustomCardPileCmd.FormationCmd(card); // 执行我们的自动化编队机制
    }
    
    // 注意：Postfix 中我们需要拦截原方法的参数 cardPlay，以及返回值 __result
    public static void Postfix(CardPlay cardPlay, ref Task __result)
    {
        var card = cardPlay.Card;
        // 1. 检查被打出的卡牌是否带有“编队”关键字
        if (card.Keywords.Contains(IndomitableKeywords.Formation))
        {
            // 2. 拦截并追加我们的异步任务
            __result = RunFormationAfterOriginal(__result, card);
        }
    }
}