using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

public static class MotivationDialoguePatches
{
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay),
        [typeof(UnplayableReason), typeof(AbstractModel)], [ArgumentType.Out, ArgumentType.Out])]
    public static class CardCanPlayPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CardModel __instance, UnplayableReason reason, ref AbstractModel? preventer)
        {
            // 仅在卡牌自身逻辑阻止打出时传递卡牌，保留原版其他原因及 Hook 阻止者。
            if (reason == UnplayableReason.BlockedByCardLogic && preventer == null && 
                !__instance.ValidateMotivationConditions())
            {
                preventer = __instance;
            }
        }
    }
    
    [HarmonyPatch("MegaCrit.Sts2.Core.Entities.Cards.UnplayableReasonExtensions", "GetPlayerDialogueLine")]
    public static class PlayerDialoguePatch
    {
        [HarmonyPostfix]
        public static void Postfix(UnplayableReason reason, AbstractModel? preventer, ref LocString? __result)
        {
            // 鼠标和手柄继续使用原版对白气泡；这里只替换缺少干劲时的通用文本。
            if (reason == UnplayableReason.BlockedByCardLogic && preventer is CardModel card && 
                !card.ValidateMotivationConditions())
            {
                __result = new LocString("combat_messages", "INDOMITABLESPIRE2-NOT_ENOUGH_MOTIVATION");
            }
        }
    }
}