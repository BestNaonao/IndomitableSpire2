using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace TashkentSpire2.TashkentSpire2Code.Config;

[HarmonyPatch]
internal static class SovetskySoyuzRunSettings
{
    private sealed record RunSetting(bool Enabled);

    private static readonly ConditionalWeakTable<StartRunLobby, LobbySettings> Lobbies = new();
    private static readonly ConditionalWeakTable<RunState, RunSetting> Runs = new();

    internal static bool IsEnabled(RunState? run)
    {
        if (run == null)
            return TashkentConfig.SpawnAncientsSovetskySoyuz;
        return Runs.TryGetValue(run, out var setting) && setting.Enabled;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(StartRunLobby), MethodType.Constructor,
        typeof(GameMode), typeof(bool), typeof(INetGameService), typeof(IStartRunLobbyListener), typeof(int))]
    private static void RegisterLobby(StartRunLobby __instance)
    {
        if (__instance.NetService is INetClientGameService client)
        {
            var settings = Lobbies.GetValue(__instance, _ => new LobbySettings(client));
            client.RegisterMessageHandler<SovetskySoyuzLobbyMessage>(settings.Receive);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.CleanUp))]
    private static void UnregisterLobby(StartRunLobby __instance)
    {
        if (__instance.NetService is INetClientGameService client && Lobbies.TryGetValue(__instance, out var settings))
        {
            client.UnregisterMessageHandler<SovetskySoyuzLobbyMessage>(settings.Receive);
            Lobbies.Remove(__instance);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(StartRunLobby), "BeginRunForAllPlayers")]
    private static void SnapshotHostSetting(StartRunLobby __instance, bool ____isBeginningRun, string seed)
    {
        if (____isBeginningRun || __instance.NetService.Type != NetGameType.Host)
            return;

        bool enabled = TashkentConfig.SpawnAncientsSovetskySoyuz;
        Lobbies.GetValue(__instance, _ => new LobbySettings(null)).StartSetting = enabled;
        // Sent before the vanilla begin-run message on the same reliable channel.
        __instance.NetService.SendMessage(new SovetskySoyuzLobbyMessage { Seed = seed, Enabled = enabled });
    }

    [HarmonyPrefix, HarmonyPatch(typeof(StartRunLobby), "HandleLobbyBeginRunMessage")]
    private static bool CheckClientSetting(StartRunLobby __instance, LobbyBeginRunMessage message, ulong senderId)
    {
        if (__instance.NetService is not INetClientGameService client)
            return true;
        if (client.NetClient?.HostNetId != senderId)
            return false;

        if (Lobbies.TryGetValue(__instance, out var settings) && settings.Pending is { } pending
            && pending.Seed == message.seed)
        {
            settings.Pending = null;
            settings.StartSetting = pending.Enabled;
            return true;
        }

        MainFile.Logger.Error("The host's Sovetsky Soyuz setting was missing when starting the run.");
        client.Disconnect(NetError.InternalError);
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewSingleplayer))]
    private static void SnapshotSingleplayer(RunState state) =>
        Runs.GetValue(state, _ => new RunSetting(TashkentConfig.SpawnAncientsSovetskySoyuz));

    [HarmonyPrefix, HarmonyPatch(typeof(RunManager), nameof(RunManager.SetUpNewMultiplayer))]
    private static void SnapshotMultiplayer(RunState state, StartRunLobby lobby)
    {
        bool enabled = Lobbies.TryGetValue(lobby, out var settings) && settings.StartSetting == true;
        Runs.GetValue(state, _ => new RunSetting(enabled));
    }

    private sealed class LobbySettings(INetClientGameService? client)
    {
        internal bool? StartSetting { get; set; }
        internal SovetskySoyuzLobbyMessage? Pending { get; set; }

        internal void Receive(SovetskySoyuzLobbyMessage message, ulong senderId)
        {
            if (StartSetting == null && client?.NetClient?.HostNetId == senderId)
                Pending = message;
        }
    }
}

public struct SovetskySoyuzLobbyMessage : INetMessage
{
    public string Seed;
    public bool Enabled;

    public readonly bool ShouldBroadcast => false;
    public readonly bool ShouldBuffer => true;
    public readonly NetTransferMode Mode => NetTransferMode.Reliable;
    public readonly LogLevel LogLevel => LogLevel.VeryDebug;

    public readonly void Serialize(PacketWriter writer)
    {
        writer.WriteString(Seed);
        writer.WriteBool(Enabled);
    }

    public void Deserialize(PacketReader reader)
    {
        Seed = reader.ReadString();
        Enabled = reader.ReadBool();
    }
}
