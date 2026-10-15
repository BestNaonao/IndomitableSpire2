using BaseLib.Audio;
using Godot;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.TestSupport;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

internal static class IndomitableVoice
{
    private const string VoiceFolder = "res://IndomitableSpire2/sfx/characters/indomitable/";
    private const string SelectTransitionFile = "denglujiemian.wav";
    private static AudioStreamPlayer? _player;
    
    internal static bool IsVoice(string path) => path.StartsWith(VoiceFolder, StringComparison.Ordinal);
    
    internal static void Play(string path, float volume)
    {
        Stop();
        
        // Character selection can play without an active run; combat voices should end with the run.
        var player = path.EndsWith(SelectTransitionFile, StringComparison.Ordinal)
            || path == SkinManager.GetDefinition(IndomitableSkin.Default).CharacterSelectSfx
            || path == SkinManager.GetDefinition(IndomitableSkin.Maid).CharacterSelectSfx
            || path == SkinManager.GetDefinition(IndomitableSkin.RaceQueen).CharacterSelectSfx
            ? ModAudio.PlaySoundGlobal(new ModSound(path), volumeMult: volume)
            : ModAudio.PlaySoundInRun(new ModSound(path), volumeMult: volume);
        
        if (player is null) return;
        
        _player = player;
        player.TreeExited += OnPlayerTreeExited;
    }
    
    internal static void Stop()
    {
        var player = _player;
        _player = null;
        if (!GodotObject.IsInstanceValid(player))
            return;
        
        player.TreeExited -= OnPlayerTreeExited;
        player.Stop();
        player.GetParent()?.RemoveChildSafely(player);
    }
    
    private static void OnPlayerTreeExited()
    {
        if (!GodotObject.IsInstanceValid(_player))
        {
            _player = null;
            return;
        }
        
        _player.TreeExited -= OnPlayerTreeExited;
        _player = null;
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