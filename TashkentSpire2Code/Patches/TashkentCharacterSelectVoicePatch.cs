using BaseLib.Audio;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.TestSupport;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

internal static class TashkentCharacterSelectVoice
{
	internal const string SelectPath = "res://TashkentSpire2/sfx/tashkent_character_select.mp3";
	internal const string TransitionPath = "res://TashkentSpire2/sfx/tashkent_character_transition.mp3";

	private static AudioStreamPlayer? _player;

	internal static void Play(float volume)
	{
		Stop();
		var player = ModAudio.PlaySoundGlobal(new ModSound(SelectPath), volumeMult: volume);
		if (player is null)
			return;

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
		// BaseLib pools AudioStreamPlayers. Removing the player returns it to the pool.
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
internal static class TashkentCharacterSelectVoicePatch
{
	[HarmonyPrefix]
	[HarmonyPriority(Priority.First)]
	private static bool Prefix(string path, float volume)
	{
		if (path == TashkentCharacterSelectVoice.TransitionPath)
			TashkentCharacterSelectVoice.Stop();

		if (path != TashkentCharacterSelectVoice.SelectPath || TestMode.IsOn)
			return true;

		TashkentCharacterSelectVoice.Play(volume);
		return false;
	}
}
