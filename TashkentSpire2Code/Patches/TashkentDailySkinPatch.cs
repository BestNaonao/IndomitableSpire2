using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
using MegaCrit.Sts2.Core.Runs;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NDailyRunScreen), "SetupLobbyParams")]
internal static class TashkentDailySkinSetupPatch
{
    private readonly record struct SelectionState(StartRunLobby? PreviousLobby);

    [HarmonyPrefix]
    private static void Prefix(StartRunLobby lobby, out SelectionState? __state)
    {
        InjectTashkentSkinSelectPanelPatch.EnterSelection(lobby, out var previous);
        __state = new SelectionState(previous);
    }

    [HarmonyFinalizer]
    private static void Finalizer(SelectionState? __state)
    {
        if (__state.HasValue)
            InjectTashkentSkinSelectPanelPatch.ExitSelection(__state.Value.PreviousLobby);
    }
}

[HarmonyPatch]
internal static class TashkentDailySkinPanelPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods() =>
    [
        AccessTools.DeclaredMethod(typeof(NDailyRunScreen), "InitializeDisplay"),
        AccessTools.DeclaredMethod(typeof(NDailyRunScreen), nameof(NDailyRunScreen.PlayerChanged))
    ];

    [HarmonyPostfix]
    private static void Postfix(NDailyRunScreen __instance, StartRunLobby? ____lobby) =>
        TashkentSkinSelection.ShowDailyPanel(__instance, ____lobby);
}

[HarmonyPatch(typeof(StartRunLobby), "SetSingleplayerAscensionAfterCharacterChanged")]
internal static class TashkentDailySkinAscensionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(StartRunLobby __instance) =>
        __instance.GameMode != GameMode.Daily || __instance.LocalPlayer.character is not TashkentCharacter;
}
