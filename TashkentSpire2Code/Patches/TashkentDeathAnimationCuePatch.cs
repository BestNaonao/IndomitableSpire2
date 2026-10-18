using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch]
public static class TashkentDeathAnimationCuePatch
{
	private static bool _watchingForRitsuLib;

	[HarmonyPrepare]
	public static bool Prepare()
	{
		if (TargetMethods().Any())
			return true;

		// PatchAll discovers this class normally. If RitsuLib loads later, retry
		// after its initializer without requiring a separate MainFile registration.
		if (!_watchingForRitsuLib)
		{
			_watchingForRitsuLib = true;
			ModManager.OnModDetected += RetryAfterModLoaded;
		}
		return false;
	}

	private static void RetryAfterModLoaded(Mod _)
	{
		if (!TargetMethods().Any())
			return;

		ModManager.OnModDetected -= RetryAfterModLoaded;
		_watchingForRitsuLib = false;
		new Harmony(MainFile.ModId)
			.CreateClassProcessor(typeof(TashkentDeathAnimationCuePatch)).Patch();
	}

	// RitsuLib's game-over compatibility also handles BaseLib characters when it
	// is installed. Resolve this optional integration without adding a dependency.
	[HarmonyTargetMethods]
	public static IEnumerable<MethodBase> TargetMethods()
	{
		Type? playback = AccessTools.TypeByName(
			"STS2RitsuLib.Scaffolding.Characters.Visuals.ModCreatureVisualPlayback");
		if (playback == null)
			yield break;

		MethodInfo? target = AccessTools.Method(playback, "TryPlayCue",
			[typeof(NCreatureVisuals), typeof(CharacterModel), typeof(string),
				typeof(ReadOnlySpan<string>), typeof(bool)]);
		if (target != null)
			yield return target;
	}

	[HarmonyPrefix]
	public static void Prefix(
		CharacterModel? character,
		ref string primaryCue,
		ReadOnlySpan<string> alternateCueNames)
	{
		if (character is TashkentCharacter && alternateCueNames.IsEmpty &&
		    string.Equals(primaryCue, "die", StringComparison.OrdinalIgnoreCase))
		{
			primaryCue = "dead";
		}
	}
}
