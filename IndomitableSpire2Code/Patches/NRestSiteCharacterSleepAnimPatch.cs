using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Random;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
public static class NRestSiteCharacterSleepAnimPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NRestSiteCharacter __instance)
    {
        // 判断是否是我们的不挠角色，如果是原版角色，返回 true，让官方的 _Ready 正常执行
        if (__instance.Player.Character is not Indomitable) return true;
        var inst = Traverse.Create(__instance);
        
        // 第一步：复刻官方的节点绑定（避免 UI 瘫痪）
        inst.Field("_controlRoot").SetValue(__instance.GetNode<Control>("ControlRoot"));
        var hitbox = __instance.GetNode<Control>("%Hitbox");
        inst.Property("Hitbox").SetValue(hitbox);
        inst.Field("_selectionReticle").SetValue(__instance.GetNode<NSelectionReticle>("%SelectionReticle"));
        inst.Field("_leftThoughtAnchor").SetValue(__instance.GetNode<Control>("%ThoughtBubbleLeft"));
        inst.Field("_rightThoughtAnchor").SetValue(__instance.GetNode<Control>("%ThoughtBubbleRight"));
        
        // 第二步：播放我们自己的 sleep 动画
        foreach (var child in __instance.GetChildren().OfType<Node2D>())
        {
            if (child.GetClass() != "SpineSprite") continue;
            var track = new MegaSprite((Variant)child).GetAnimationState().SetAnimation("sleep");
            track?.SetTrackTime(track.GetAnimationEnd() * Rng.Chaotic.NextFloat());
        }
        
        // 第三步：复刻官方的鼠标交互信号连接
        // 因为 OnFocus 等方法是 private，我们使用 Godot 的 Callable 字符串绑定法
        hitbox.Connect(Control.SignalName.FocusEntered, new Callable(__instance, "OnFocus"));
        hitbox.Connect(Control.SignalName.FocusExited, new Callable(__instance, "OnUnfocus"));
        hitbox.Connect(Control.SignalName.MouseEntered, new Callable(__instance, "OnFocus"));
        hitbox.Connect(Control.SignalName.MouseExited, new Callable(__instance, "OnUnfocus"));
        
        // 返回 false，告诉 Harmony 拦截掉官方的 _Ready 方法，不要再往下执行了！
        return false;
    }
}