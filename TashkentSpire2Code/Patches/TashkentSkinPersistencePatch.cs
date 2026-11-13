using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Config;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NGame))]
internal static class TashkentSkinPersistencePatch
{
    [HarmonyPostfix, HarmonyPatch(nameof(NGame.StartNewSingleplayerRun))]
    private static void ObserveSingleplayer(Task<RunState> __result, bool shouldSave)
    {
        if (ShouldRemember(shouldSave))
            _ = TaskHelper.RunSafely(RememberAfterLaunch(__result, localNetId: null));
    }

    [HarmonyPostfix, HarmonyPatch(nameof(NGame.StartNewMultiplayerRun))]
    private static void ObserveMultiplayer(Task<RunState> __result, StartRunLobby lobby, bool shouldSave)
    {
        if (ShouldRemember(shouldSave))
            _ = TaskHelper.RunSafely(RememberAfterLaunch(__result, lobby.NetService.NetId));
    }

    private static bool ShouldRemember(bool shouldSave)
        => shouldSave && !TestMode.IsOn && TashkentConfig.EnableSpineModels;

    private static async Task RememberAfterLaunch(Task<RunState> launch, ulong? localNetId)
    {
        RunState state;
        try
        {
            state = await launch;
        }
        catch (Exception)
        {
            // The original caller still handles the launch failure. No preference
            // is written if the player never entered the run.
            return;
        }

        if (state.GameMode != GameMode.Standard) return;
        var player = localNetId.HasValue ? state.GetPlayer(localNetId.Value) : state.Players.FirstOrDefault();
        if (player?.Character is not TashkentCharacter skin || TashkentConfig.LastPlayedSkinId == skin.Id.Entry)
            return;

        TashkentConfig.LastPlayedSkinId = skin.Id.Entry;
        ModConfig.SaveDebounced<TashkentConfig>(0);
    }
}
