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
            DamageOverTimeRegistry.Instance.GetOrCreateForeground(__instance, provider.DamageTypeId);
        
        // 挂载 TreeExiting 委托，将生命周期绑定到原生的 Godot 信号上，自动从 Registry 的字典中剔除它
        var creatureField = AccessTools.Field(typeof(NHealthBar), "_creature");
        __instance.TreeExiting += () => 
            DamageOverTimeRegistry.Instance.Cleanup(__instance, ((Creature)creatureField.GetValue(__instance)!).Name);
    }

    [HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
    [HarmonyPrefix]
    private static bool RefreshForeground_Prefix(NHealthBar __instance, 
        Creature ____creature, Control ____hpForeground, Control ____poisonForeground, Control ____doomForeground)
    {
        var maxFgWidthProp = AccessTools.Property(typeof(NHealthBar), "MaxFgWidth");
        var maxFgWidth = (float)maxFgWidthProp!.GetValue(__instance)!;
        var getFgWidthMethod = AccessTools.Method(typeof(NHealthBar), "GetFgWidth", [typeof(int), typeof(float)]);
        
        // 死亡状态清理
        if (____creature.CurrentHp <= 0)
        {
            DamageOverTimeRegistry.Instance.HideAll(__instance);
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
        
        var remainingHp = ____creature.CurrentHp;
        var currentHpWidth = (float)getFgWidthMethod.Invoke(__instance, [remainingHp, maxFgWidth])!;
        
        // 重置所有前景
        ____hpForeground.Visible = true;
        ____hpForeground.OffsetRight = currentHpWidth - maxFgWidth;
        foreach (var control in foregroundControls.Values) control.Visible = false;
        ____poisonForeground.Visible = ____doomForeground.Visible = false;
        
        // 无敌状态
        if (____creature.ShowsInfiniteHp)
        {
            ____hpForeground.SelfModulate = new Color("C5BBED");
            return false;
        }
        
        // ========== 循环计算致死性与显示条 ==========
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
            
            // 如果伤害源致死，则占据全部剩余血条，并隐藏剩余HP条
            if (remainingHp > 0) continue;
            ____hpForeground.Visible = false;
            break;
        }
        if (____hpForeground.Visible) ____hpForeground.OffsetRight = currentHpWidth - maxFgWidth;
        
        // 2. 手动渲染灾厄并拦截原方法
        if (____creature.GetPowerAmount<DoomPower>() is var doomAmount and > 0 && remainingHp > 0)
        {
            ____doomForeground.Visible = true;
            var doomHpWidth = (float)getFgWidthMethod.Invoke(__instance, [doomAmount, maxFgWidth])!;
            ____doomForeground.OffsetRight = Math.Min(currentHpWidth - maxFgWidth, doomHpWidth - maxFgWidth);
        }

        return false;
    }

    [HarmonyPatch(typeof(NHealthBar), "RefreshText")]
    [HarmonyPrefix]
    private static bool RefreshText_Prefix(NHealthBar __instance, 
        Creature ____creature, MegaLabel ____hpLabel, TextureRect ____infinityTex, Control ____doomForeground)
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
        
        var (fontColor, outlineColor) =
            lethalProvider != null  // 持续伤害致死
                ? (lethalProvider.LethalFontColor, lethalProvider.LethalOutlineColor)
                : doomAmount > 0 && doomAmount >= remainingHp   // 灾厄致死
                    ? (new Color("FB8DFF"), new Color("2D1263"))
                    : ____creature.Block > 0    // 有无格挡
                        ? (DefaultFontColor, BlockOutlineColor)
                        : (DefaultFontColor, DefaultOutlineColor);

        ____hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, fontColor);
        ____hpLabel.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, outlineColor);
        ____hpLabel.SetTextAutoSize($"{____creature.CurrentHp}/{____creature.MaxHp}");
        
        return false;
    }
}