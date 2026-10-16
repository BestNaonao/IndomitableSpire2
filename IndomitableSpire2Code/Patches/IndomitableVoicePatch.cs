using BaseLib.Audio;
using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.TestSupport;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

internal static class IndomitableVoice
{
    private const string VoiceFolder = "res://IndomitableSpire2/sfx/characters/indomitable/";
    private const string SelectTransitionFile = "denglujiemian.wav";
    private static VoiceSlot? _sharedVoice;
    private static readonly Dictionary<ulong, VoiceSlot> PlayerVoices = new();
    
    // SfxCmd.Play 到 NAudioManager.PlayOneShot 是同步调用；仅在这次调用中传递出牌玩家。
    [ThreadStatic]
    private static ulong? _cardLineSpeaker;
    
    internal static bool IsVoice(string path) => path.StartsWith(VoiceFolder, StringComparison.Ordinal);
    
    // 为卡牌语音提供的 SfxCmd.Play 的包装，按线程记录 _cardLineSpeaker。
    internal static void PlayCardLine(ulong speakerId, string path, float volume)
    {
        var previousSpeaker = _cardLineSpeaker;
        try
        {
            _cardLineSpeaker = speakerId;
            SfxCmd.Play(path, volume);
        }
        finally
        {
            _cardLineSpeaker = previousSpeaker;
        }
    }
    
    internal static void Play(string path, float volume)
    {
        // 选人等非卡牌语音仍使用统一通道；开启选项后，卡牌特殊语音按出牌玩家分组。
        var speakerId = IndomitableConfiguration.IsolateSpecialVoiceByPlayer ? _cardLineSpeaker : null;
        if (speakerId is { } id)
        {
            StopSlot(_sharedVoice);
            _sharedVoice = null;
            if (PlayerVoices.Remove(id, out var previous))
                StopSlot(previous);
        }
        else
        {
            Stop();
        }
        
        // 选人界面可能没有正在运行的对局；战斗语音则跟随本局的节点结束。
        var player = path.EndsWith(SelectTransitionFile, StringComparison.Ordinal)
            || path == SkinManager.GetDefinition(IndomitableSkin.Default).CharacterSelectSfx
            || path == SkinManager.GetDefinition(IndomitableSkin.Maid).CharacterSelectSfx
            || path == SkinManager.GetDefinition(IndomitableSkin.RaceQueen).CharacterSelectSfx
            ? ModAudio.PlaySoundGlobal(new ModSound(path), volumeMult: volume)
            : ModAudio.PlaySoundInRun(new ModSound(path), volumeMult: volume);
        
        if (player is null) return;
        
        var slot = new VoiceSlot(player);
        slot.OnExited = () => OnPlayerTreeExited(slot, speakerId);
        if (speakerId is { } playerId)
            PlayerVoices[playerId] = slot;
        else
            _sharedVoice = slot;
        player.TreeExited += slot.OnExited;
    }
    
    internal static void Stop()
    {
        var shared = _sharedVoice;
        _sharedVoice = null;
        StopSlot(shared);
        
        // 配置可能在播放中切换；清理全部玩家通道以维持统一打断语义。
        var playerSlots = PlayerVoices.Values.ToArray();
        PlayerVoices.Clear();
        foreach (var slot in playerSlots)
            StopSlot(slot);
    }
    
    private static void StopSlot(VoiceSlot? slot)
    {
        if (slot is null || !GodotObject.IsInstanceValid(slot.Player))
            return;
        slot.Player.TreeExited -= slot.OnExited;
        slot.Player.Stop();
        slot.Player.GetParent()?.RemoveChildSafely(slot.Player);
    }
    
    private static void OnPlayerTreeExited(VoiceSlot slot, ulong? speakerId)
    {
        if (GodotObject.IsInstanceValid(slot.Player))
            slot.Player.TreeExited -= slot.OnExited;
        
        // BaseLib 会回收播放器，所以只移除当前仍指向这个播放实例的通道。
        if (speakerId is { } id)
        {
            if (PlayerVoices.TryGetValue(id, out var current) && ReferenceEquals(current, slot))
                PlayerVoices.Remove(id);
        }
        else if (ReferenceEquals(_sharedVoice, slot))
        {
            _sharedVoice = null;
        }
    }
    
    private sealed class VoiceSlot(AudioStreamPlayer player)
    {
        internal AudioStreamPlayer Player { get; } = player;
        internal Action OnExited { get; set; } = null!;
    }
}

[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayOneShot),
    typeof(string), typeof(Dictionary<string, float>), typeof(float))]
public static class IndomitableVoicePatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(string path, float volume)
    {
        if (!IndomitableVoice.IsVoice(path) || TestMode.IsOn || !ResourceLoader.Exists(path))
            return true;
        IndomitableVoice.Play(path, volume);
        return false;
    }
}

// 多人读档界面会直接播放存档角色的选人语音，退出时也要清理该语音。
[HarmonyPatch(typeof(NMultiplayerLoadGameScreen), nameof(NMultiplayerLoadGameScreen.OnSubmenuClosed))]
public static class MultiplayerLoadVoicePatch
{
    [HarmonyPrefix]
    public static void OnSubmenuClosedPrefix() => IndomitableVoice.Stop();
}