using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

/// <summary>目标定位工具类。把标准、极速、恐惧、自定义四个模式的选人界面集中定义一处，各补丁用 Methods("方法名") 批量取目标。</summary>
internal static class SkinLobbyScreens
{
    private static readonly Type[] Screens =
        [typeof(NCharacterSelectScreen), typeof(NBlitzScreen), typeof(NDreadScreen), typeof(NCustomRunScreen)];
    
    internal static IEnumerable<MethodBase> Methods(string name) =>
        Screens.Select(screen => AccessTools.DeclaredMethod(screen, name));
}