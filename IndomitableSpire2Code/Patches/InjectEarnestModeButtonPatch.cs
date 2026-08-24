using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// 将认真模式按钮挂载到 NCombatUi，使其与手牌、能量和结束回合按钮共享战斗生命周期。
/// </summary>
[HarmonyPatch(typeof(NCombatUi))]
public static class InjectEarnestModeButtonPatch
{
    private const string ScenePath = "res://IndomitableSpire2/scenes/combat/earnest_mode_button.tscn";
    
    [HarmonyPatch(nameof(NCombatUi._Ready))]
    [HarmonyPostfix]
    private static void ReadyPostfix(NCombatUi __instance)
    {
        if (__instance.GetNodeOrNull<EarnestModeButton>("EarnestModeButton") is not null) return;
        var scene = ResourceLoader.Load<PackedScene>(ScenePath);
        if (scene is null)
        {
            MainFile.Logger.Error($"Could not load EarnestModeButton scene: {ScenePath}");
            return;
        }
        __instance.AddChildSafely(scene.Instantiate<EarnestModeButton>());
        MainFile.Logger.Info("Injected EarnestModeButton into NCombatUi.");
    }
    
    [HarmonyPatch(nameof(NCombatUi.Activate))]
    [HarmonyPostfix]
    private static void ActivatePostfix(NCombatUi __instance, CombatState state)
    {
        var button = __instance.GetNodeOrNull<EarnestModeButton>("EarnestModeButton");
        if (button is null)
        {
            MainFile.Logger.Error("EarnestModeButton was not present when NCombatUi.Activate ran.");
            return;
        }
        button.Initialize(state);
    }
}
