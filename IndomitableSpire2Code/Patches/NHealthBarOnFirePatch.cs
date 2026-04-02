using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.addons.mega_text;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

// [HarmonyPatch]
// public static class NHealthBarOnFirePatch
// {
    // private static readonly Color _fireForegroundColor = new("FFA200");
    // private static readonly Color _fireLethalFontColor = new("FFD700");
    // private static readonly Color _fireLethalOutlineColor = new("4A3C00");
    //
    // private static readonly Dictionary<NHealthBar, Control> _onFireForegroundMap = new();
    //
    // private static bool IsOnFireLethal(Creature? creature, int onFireDamage)
    // {
    //     if (creature is not { CurrentHp: > 0 }) return false;
    //     if (onFireDamage <= 0) return false;
    //     return onFireDamage >= creature.CurrentHp;
    // }
    //
    // private static Control GetOrCreateOnFireForeground(NHealthBar healthBar)
    // {
    //     if (_onFireForegroundMap.TryGetValue(healthBar, out var existing))
    //         return existing;
    //     
    //     var mask = healthBar.GetNode<Control>("%HpForegroundContainer/Mask");
    //     var poisonForeground = healthBar.GetNode<Control>("%PoisonForeground");
    //     
    //     if (poisonForeground == null || mask == null)
    //     {
    //         MainFile.Logger.Error("HealthBar Patch: Could not find PoisonForeground or Mask");
    //         return new Control();
    //     }
    //     
    //     var existingOnFire = mask.GetNodeOrNull<Control>("OnFireForeground");
    //     if (existingOnFire != null)
    //     {
    //         _onFireForegroundMap[healthBar] = existingOnFire;
    //         return existingOnFire;
    //     }
    //     
    //     var onFireForeground = new NinePatchRect
    //     {
    //         Name = "OnFireForeground",
    //         Texture = (Texture2D)((NinePatchRect)poisonForeground).Get("texture"),
    //         PatchMarginLeft = ((NinePatchRect)poisonForeground).PatchMarginLeft,
    //         PatchMarginTop = ((NinePatchRect)poisonForeground).PatchMarginTop,
    //         PatchMarginRight = ((NinePatchRect)poisonForeground).PatchMarginRight,
    //         PatchMarginBottom = ((NinePatchRect)poisonForeground).PatchMarginBottom,
    //         Visible = false,
    //         SelfModulate = _fireForegroundColor,
    //         ClipContents = true
    //     };
    //     
    //     mask.AddChild(onFireForeground);
    //     
    //     var poisonIndex = mask.GetChildren().ToList().IndexOf(poisonForeground);
    //     if (poisonIndex >= 0)
    //     {
    //         mask.MoveChild(onFireForeground, poisonIndex + 1);
    //     }
    //     
    //     _onFireForegroundMap[healthBar] = onFireForeground;
    //     MainFile.Logger.Info("HealthBar Patch: Created OnFireForeground under Mask");
    //     
    //     return onFireForeground;
    // }
    //
    // private static void CleanupOnFireForeground(NHealthBar healthBar)
    // {
    //     if (_onFireForegroundMap.TryGetValue(healthBar, out var onFireForeground))
    //     {
    //         onFireForeground.QueueFree();
    //         _onFireForegroundMap.Remove(healthBar);
    //     }
    // }
    //
    // [HarmonyPatch(typeof(NHealthBar), "_Ready")]
    // [HarmonyPostfix]
    // private static void Ready_Postfix(NHealthBar __instance)
    // {
    //     GetOrCreateOnFireForeground(__instance);
    // }
    //
    // [HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
    // [HarmonyPrefix]
    // private static bool RefreshForeground_Prefix(
    //     NHealthBar __instance, 
    //     Creature ____creature, 
    //     Control ____hpForeground, 
    //     Control ____poisonForeground, 
    //     Control ____doomForeground)
    // {
    //     var maxFgWidthProp = AccessTools.Property(typeof(NHealthBar), "MaxFgWidth");
    //     var maxFgWidth = (float)maxFgWidthProp!.GetValue(__instance)!;
    //     var getFgWidthMethod = AccessTools.Method(typeof(NHealthBar), "GetFgWidth", [typeof(int), typeof(float)]);
    //     
    //     if (____creature.IsDead || ____creature.CurrentHp <= 0)
    //     {
    //         CleanupOnFireForeground(__instance);
    //         return true;
    //     }
    //     
    //     var onFirePower = ____creature.GetPower<OnFirePower>();
    //     var onFireDamage = onFirePower?.GetNextDamage() ?? 0;
    //     
    //     // ★ 没有 OnFire 时完全交给原方法处理
    //     if (onFirePower == null || onFireDamage <= 0)
    //     {
    //         return true;
    //     }
    //     
    //     var onFireForeground = GetOrCreateOnFireForeground(__instance);
    //     
    //     var poisonPower = ____creature.GetPower<MegaCrit.Sts2.Core.Models.Powers.PoisonPower>();
    //     var poisonDamage = poisonPower?.CalculateTotalDamageNextTurn() ?? 0;
    //     
    //     var doomAmount = ____creature.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.DoomPower>();
    //     
    //     var currentHpWidth = (float)getFgWidthMethod.Invoke(__instance, [____creature.CurrentHp, maxFgWidth])!;
    //     var num1 = currentHpWidth - maxFgWidth;
    //     
    //     if (____creature.ShowsInfiniteHp)
    //     {
    //         ____hpForeground.SelfModulate = new Color("C5BBED");
    //         ____hpForeground.Visible = true;
    //         ____hpForeground.OffsetRight = num1;
    //         ____poisonForeground.Visible = false;
    //         ____doomForeground.Visible = false;
    //         onFireForeground.Visible = false;
    //         return false;
    //     }
    //     
    //     ____hpForeground.Visible = true;
    //     ____hpForeground.OffsetRight = num1;
    //     
    //     ____poisonForeground.Visible = false;
    //     ____doomForeground.Visible = false;
    //     onFireForeground.Visible = false;
    //     
    //     var isPoisonLethal = poisonDamage > 0 && poisonPower != null && poisonDamage >= ____creature.CurrentHp;
    //     var isFireLethal = IsOnFireLethal(____creature, onFireDamage);
    //     var isDoomLethal = doomAmount > 0 && doomAmount >= ____creature.CurrentHp - poisonDamage;
    //     
    //     // 1. 中毒致命（最高优先级）
    //     if (isPoisonLethal)
    //     {
    //         ____poisonForeground.Visible = true;
    //         ____poisonForeground.OffsetLeft = 0.0f;
    //         ____poisonForeground.OffsetRight = num1;
    //         ____hpForeground.Visible = false;
    //         onFireForeground.Visible = false;
    //         ____doomForeground.Visible = false;
    //         return false;
    //     }
    //     
    //     // 2. OnFire 致命
    //     if (isFireLethal)
    //     {
    //         onFireForeground.Visible = true;
    //         onFireForeground.OffsetLeft = 0.0f;
    //         onFireForeground.OffsetRight = num1;
    //         ____hpForeground.Visible = false;
    //         ____poisonForeground.Visible = false;
    //         ____doomForeground.Visible = false;
    //         MainFile.Logger.Info($"HealthBar Patch: OnFire Lethal ({onFireDamage} >= {____creature.CurrentHp})");
    //         return false;
    //     }
    //     
    //     // 3. 灾厄致命（让原方法处理）
    //     if (isDoomLethal)
    //     {
    //         return true;
    //     }
    //     
    //     // ========== 非致命：显示预期损失 ==========
    //     var poisonRemainingHp = Math.Max(0, ____creature.CurrentHp - poisonDamage);
    //     var poisonRemainingHpWidth = (float)getFgWidthMethod.Invoke(__instance, [poisonRemainingHp, maxFgWidth])!;
    //     var fireRemainingHp = Math.Max(0, ____creature.CurrentHp - poisonDamage - onFireDamage);
    //     var fireRemainingHpWidth = (float)getFgWidthMethod.Invoke(__instance, [fireRemainingHp, maxFgWidth])!;
    //     
    //     MainFile.Logger.Info($"HealthBar Patch: CurrentHp={____creature.CurrentHp}, Poison={poisonDamage}, OnFire={onFireDamage}, Remaining={fireRemainingHp}, RemainingWidth={fireRemainingHpWidth}");
    //     
    //     // 显示中毒预期伤害
    //     if (poisonDamage > 0 && poisonPower != null)
    //     {
    //         ____poisonForeground.Visible = true;
    //         var poisonPatchMarginLeft = ((NinePatchRect)____poisonForeground).PatchMarginLeft;
    //         ____poisonForeground.OffsetLeft = Math.Max(0.0f, poisonRemainingHpWidth - poisonPatchMarginLeft);
    //         ____poisonForeground.OffsetRight = num1;
    //     }
    //     
    //     // 显示 OnFire 预期伤害
    //     onFireForeground.Visible = true;
    //     var onFirePatchMarginLeft = ((NinePatchRect)onFireForeground).PatchMarginLeft;
    //     onFireForeground.OffsetLeft = Math.Max(0.0f, fireRemainingHpWidth - onFirePatchMarginLeft);
    //     onFireForeground.OffsetRight = poisonRemainingHpWidth - maxFgWidth;
    //     ____hpForeground.Visible = true;
    //     ____hpForeground.OffsetRight = fireRemainingHpWidth - maxFgWidth;
    //     
    //     MainFile.Logger.Info($"HealthBar Patch: OnFire Visible, OffsetLeft={onFireForeground.OffsetLeft}");
    //     
    //     return false;
    // }
    //
    // [HarmonyPatch(typeof(NHealthBar), "RefreshText")]
    // [HarmonyPrefix]
    // private static bool RefreshText_Prefix(
    //     NHealthBar __instance, 
    //     Creature ____creature,
    //     MegaLabel ____hpLabel, 
    //     TextureRect ____infinityTex, 
    //     Control ____doomForeground)
    // {
    //     if (____creature.IsDead || ____creature.CurrentHp <= 0)
    //         return true;
    //     
    //     ____doomForeground.Modulate = ____creature.ShowsInfiniteHp ? Colors.Transparent : Colors.White;
    //     ____infinityTex.Visible = ____creature.ShowsInfiniteHp;
    //     ____hpLabel.Visible = !____creature.ShowsInfiniteHp;
    //     
    //     if (____creature.ShowsInfiniteHp)
    //         return false;
    //     
    //     var onFirePower = ____creature.GetPower<OnFirePower>();
    //     var onFireDamage = onFirePower?.GetNextDamage() ?? 0;
    //     
    //     var poisonPower = ____creature.GetPower<MegaCrit.Sts2.Core.Models.Powers.PoisonPower>();
    //     var poisonDamage = poisonPower?.CalculateTotalDamageNextTurn() ?? 0;
    //     
    //     var doomAmount = ____creature.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.DoomPower>();
    //     
    //     Color fontColor;
    //     Color outlineColor;
    //     
    //     // ========== 颜色优先级：中毒 > OnFire > 灾厄 > 默认 ==========
    //     if (poisonDamage > 0 && poisonPower != null && poisonDamage >= ____creature.CurrentHp)
    //     {
    //         // 中毒致命
    //         fontColor = new Color("76FF40");
    //         outlineColor = new Color("074700");
    //     }
    //     else if (IsOnFireLethal(____creature, onFireDamage))
    //     {
    //         // OnFire 致命
    //         fontColor = _fireLethalFontColor;
    //         outlineColor = _fireLethalOutlineColor;
    //     }
    //     else if (doomAmount > 0 && doomAmount >= ____creature.CurrentHp - poisonDamage)
    //     {
    //         // 灾厄致命
    //         fontColor = new Color("FB8DFF");
    //         outlineColor = new Color("2D1263");
    //     }
    //     else if (____creature.Block <= 0)
    //     {
    //         // 无格挡
    //         fontColor = new Color("FFFFFF");
    //         outlineColor = new Color("900000");
    //     }
    //     else
    //     {
    //         // 有格挡
    //         fontColor = new Color("FFFFFF");
    //         outlineColor = new Color("1B3045");
    //     }
    //
    //     ____hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, fontColor);
    //     ____hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, outlineColor);
    //     ____hpLabel.SetTextAutoSize($"{____creature.CurrentHp}/{____creature.MaxHp}");
    //     
    //     return false;
    // }
// }