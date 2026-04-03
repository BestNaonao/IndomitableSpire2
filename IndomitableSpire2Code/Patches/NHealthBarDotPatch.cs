using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.addons.mega_text;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Registries;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch]
public static class NHealthBarDotPatch
{
    private static readonly Color DefaultFontColor = new("FFFFFF");
    private static readonly Color DefaultOutlineColor = new("900000");
    private static readonly Color BlockOutlineColor = new("1B3045");
    
    [HarmonyPatch(typeof(NHealthBar), "_Ready")]
    [HarmonyPostfix]
    private static void Ready_Postfix(NHealthBar __instance)
    {
        // 预创建所有注册的前景控件
        foreach (var provider in DamageOverTimeRegistry.Instance.GetProviders())
        {
            DamageOverTimeRegistry.Instance.GetOrCreateForeground(__instance, provider.DamageTypeId);
        }
    }

    [HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
    [HarmonyPrefix]
    private static bool RefreshForeground_Prefix(
        NHealthBar __instance, 
        Creature ____creature, 
        Control ____hpForeground, 
        Control ____poisonForeground, 
        Control ____doomForeground)
    {
        var maxFgWidthProp = AccessTools.Property(typeof(NHealthBar), "MaxFgWidth");
        var maxFgWidth = (float)maxFgWidthProp!.GetValue(__instance)!;
        var getFgWidthMethod = AccessTools.Method(typeof(NHealthBar), "GetFgWidth", [typeof(int), typeof(float)]);
        
        // 死亡状态清理
        if (____creature.IsDead || ____creature.CurrentHp <= 0)
        {
            DamageOverTimeRegistry.Instance.Cleanup(__instance);
            return true;
        }
        
        // 获取所有激活的持续伤害源，没有持续伤害时交给原方法
        var dotSources = DamageOverTimeRegistry.Instance.GetActiveDotSources(____creature);
        if (dotSources.Count == 0) return true;
        
        // 获取所有前景控件
        var foregroundControls = dotSources.ToDictionary(
            x => x.Provider.DamageTypeId,
            x => DamageOverTimeRegistry.Instance.GetOrCreateForeground(__instance, x.Provider.DamageTypeId)
        );
        
        var currentHpWidth = (float)getFgWidthMethod.Invoke(__instance, [____creature.CurrentHp, maxFgWidth])!;
        
        // 无敌状态
        if (____creature.ShowsInfiniteHp)
        {
            ____hpForeground.SelfModulate = new Color("C5BBED");
            ____hpForeground.Visible = true;
            ____hpForeground.OffsetRight = currentHpWidth - maxFgWidth;
            HideAllDotForegrounds(foregroundControls, ____poisonForeground, ____doomForeground);
            return false;
        }
        
        // 重置所有前景
        ____hpForeground.Visible = true;
        ____hpForeground.OffsetRight = currentHpWidth - maxFgWidth;
        HideAllDotForegrounds(foregroundControls, ____poisonForeground, ____doomForeground);
        
        // ========== 循环计算致死性与显示条 ==========
        var remainingHp = ____creature.CurrentHp;
        
        foreach (var (provider, damage) in dotSources)
        {
            // 寻找并显示伤害条，计算当前伤害触发前剩余血量的宽度（右位置）
            var currentForeground = foregroundControls[provider.DamageTypeId];
            currentForeground.Visible = true;
            currentForeground.OffsetRight = currentHpWidth - maxFgWidth;
            
            // 计算剩余伤害后的血量和新的剩余血条长度
            remainingHp = Math.Max(0, remainingHp - damage);
            currentHpWidth = (float)getFgWidthMethod.Invoke(__instance, [remainingHp, maxFgWidth])!;
            var patchMarginLeft = ((NinePatchRect)currentForeground).PatchMarginLeft;
            currentForeground.OffsetLeft = Math.Max(0.0f, currentHpWidth - patchMarginLeft);
            MainFile.Logger.Info($"DebugPatch {provider.DamageTypeId}: OffsetLeft={currentForeground.OffsetLeft}, OffsetRight={currentForeground.OffsetRight}, MaxFgWidth={maxFgWidth}");
            
            // 如果伤害源致死，则占据全部剩余血条，并隐藏剩余HP条
            if (remainingHp > 0) continue;
            ____hpForeground.Visible = false;
            break;
        }
        if (____hpForeground.Visible) ____hpForeground.OffsetRight = currentHpWidth - maxFgWidth;
        
        var mask = __instance.GetNodeOrNull<Control>("%HpForegroundContainer/Mask");
        foreach (var child in mask.GetChildren().ToList()) MainFile.Logger.Info($"DebugPatch: {child.Name}");
        
        // 2. 手动渲染灾厄并拦截原方法
        if (____creature.GetPowerAmount<DoomPower>() is var doomAmount and > 0 && remainingHp > 0)
        {
            ____doomForeground.Visible = true;
            var doomHpWidth = (float)getFgWidthMethod.Invoke(__instance, [doomAmount, maxFgWidth])!;
            ____doomForeground.OffsetRight = Math.Min(currentHpWidth - maxFgWidth, doomHpWidth - maxFgWidth);
            MainFile.Logger.Info($"DebugPatch Doom Provided: OffsetLeft={____doomForeground.OffsetLeft}, OffsetRight={____doomForeground.OffsetRight}, MaxFgWidth={maxFgWidth}");
        }

        return false;
    }

    [HarmonyPatch(typeof(NHealthBar), "RefreshText")]
    [HarmonyPrefix]
    private static bool RefreshText_Prefix(
        NHealthBar __instance, 
        Creature ____creature,
        MegaLabel ____hpLabel, 
        TextureRect ____infinityTex, 
        Control ____doomForeground)
    {
        if (____creature.IsDead || ____creature.CurrentHp <= 0)
            return true;
        
        ____doomForeground.Modulate = ____creature.ShowsInfiniteHp ? Colors.Transparent : Colors.White;
        ____infinityTex.Visible = ____creature.ShowsInfiniteHp;
        ____hpLabel.Visible = !____creature.ShowsInfiniteHp;
        
        if (____creature.ShowsInfiniteHp)
            return false;
        
        var dotSources = DamageOverTimeRegistry.Instance.GetActiveDotSources(____creature);
        var doomAmount = ____creature.GetPowerAmount<DoomPower>();
        
        Color fontColor;
        Color outlineColor;
        
        // ========== 循环检查致死性 ==========
        var remainingHp = ____creature.CurrentHp;
        IDamageOverTimeProvider? lethalProvider = null;
        
        foreach (var (provider, damage) in dotSources)
        {
            if (damage >= remainingHp)
            {
                lethalProvider = provider;
                break;
            }
            remainingHp -= damage;
        }
        
        // 检查灾厄致死
        var isDoomLethal = doomAmount > 0 && doomAmount >= remainingHp;
        
        if (lethalProvider != null)
        {
            // 持续伤害致死
            fontColor = lethalProvider.LethalFontColor;
            outlineColor = lethalProvider.LethalOutlineColor;
            MainFile.Logger.Info($"DotPatch Text: {lethalProvider.DisplayName} Lethal");
        }
        else if (isDoomLethal)
        {
            // 灾厄致死
            fontColor = new Color("FB8DFF");
            outlineColor = new Color("2D1263");
        }
        else if (____creature.Block <= 0)
        {
            // 无格挡
            fontColor = DefaultFontColor;
            outlineColor = DefaultOutlineColor;
        }
        else
        {
            // 有格挡
            fontColor = DefaultFontColor;
            outlineColor = BlockOutlineColor;
        }

        ____hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, fontColor);
        ____hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, outlineColor);
        ____hpLabel.SetTextAutoSize($"{____creature.CurrentHp}/{____creature.MaxHp}");
        
        return false;
    }
    
    private static void HideAllDotForegrounds(
        Dictionary<string, Control> dotControls, 
        Control poisonForeground, 
        Control doomForeground)
    {
        foreach (var control in dotControls.Values) control.Visible = false;
        poisonForeground.Visible = false;
        doomForeground.Visible = false;
    }
}