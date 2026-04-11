using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

public static class AnyPlayerTargetingFixes
{
    // ==========================================
    // 补丁 1：修复底层数据验证（拒绝盲拖 null，必须有明确的玩家目标）
    // ==========================================
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.IsValidTarget))]
    public static class CardModelAnyPlayerTargetFixPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
        {
            if (__instance.TargetType != TargetType.AnyPlayer) return true;
            __result = target is { IsAlive: true, IsPlayer: true };
            return false;
        }
    }

    // ==========================================
    // 补丁 2：修复 UI 连线（让 AnyPlayer 也能拉出单体指向箭头）
    // ==========================================
    [HarmonyPatch(typeof(NMouseCardPlay), "TargetSelection")]
    public static class NMouseCardPlayTargetSelectionPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(NMouseCardPlay __instance, TargetMode targetMode, ref Task __result)
        {
            var card = __instance.Holder.CardModel;
            // 拦截 AnyPlayer 的逻辑
            if (card is not { TargetType: TargetType.AnyPlayer }) return true;
            // 反射调用私有方法 SingleCreatureTargeting 拉出连线
            var inst = Traverse.Create(__instance);
            inst.Method("TryShowEvokingOrbs").GetValue();
            inst.Property("CardNode")?.GetValue<NCard>().CardHighlight.AnimFlash();
            __result = inst.Method("SingleCreatureTargeting", targetMode, card.TargetType).GetValue<Task>();
            return false; // 拦截原版
        }
    }

    // ==========================================
    // 补丁 3：修复结算逻辑（防止好不容易选中的 Target 被官方强制抹除为 null）
    // ==========================================
    [HarmonyPatch(typeof(NCardPlay), "TryPlayCard")]
    public static class NCardPlayTryPlayCardPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(NCardPlay __instance, Creature? target)
        {
            var card = __instance.Holder.CardModel;
            // 只拦截 AnyPlayer，其他继续走原版逻辑
            if (card is not { TargetType: TargetType.AnyPlayer }) return true;
            // 1. 如果拉断了线（target == null），取消打出
            if (target == null)
            {
                __instance.CancelPlayCard();
                return false;
            }

            // 2. 检查目标是否合法（会调用我们上面的 补丁 1）
            if (!card.CanPlayTargeting(target))
            {
                Traverse.Create(__instance).Method("CannotPlayThisCardFtueCheck", card).GetValue();
                __instance.CancelPlayCard();
                return false;
            }

            // 3. 核心修复：不抹除 target，直接带着 target 真正打出！
            var inst = Traverse.Create(__instance);
            inst.Field("_isTryingToPlayCard").SetValue(true);
            // 【核心】：直接用真实的 target，绕过原版的 flag1 抹除逻辑！
            var playedSuccessfully = card.TryManualPlay(target);
            inst.Field("_isTryingToPlayCard").SetValue(false);

            if (playedSuccessfully)
            {
                inst.Method("AutoDisableCannotPlayCardFtueCheck").GetValue();
                    
                // 把卡牌居中展示
                if (__instance.Holder.IsInsideTree())
                {
                    var size = __instance.GetViewport().GetVisibleRect().Size;
                    __instance.Holder.SetTargetPosition(new Godot.Vector2(size.X / 2f, size.Y - __instance.Holder.Size.Y));
                }
                    
                inst.Method("Cleanup", true).GetValue();
                NCombatRoom.Instance?.Ui.Hand.TryGrabFocus();
            }
            else
            {
                __instance.CancelPlayCard();
            }

            return false; // 拦截原版

        }
    }
}