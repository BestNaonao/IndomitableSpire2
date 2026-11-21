using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character;

/// <summary>
/// 选人语音使用全局播放器，关闭各模式大厅或多人读档界面时统一停止。
/// </summary>
[HarmonyPatch]
public static class StopCharacterVoiceOnClosePatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
        new[] { typeof(NCharacterSelectScreen), typeof(NBlitzScreen), typeof(NDreadScreen),
            typeof(NCustomRunScreen), typeof(NMultiplayerLoadGameScreen) }
            .Select(screen => AccessTools.DeclaredMethod(screen, nameof(NCharacterSelectScreen.OnSubmenuClosed)));
    
    [HarmonyPrefix]
    private static void Prefix() => IndomitableVoice.Stop();
}