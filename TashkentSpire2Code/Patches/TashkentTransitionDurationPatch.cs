using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

/// <summary>
/// Gives the local Tashkent character-select transition enough time to reproduce
/// the reference animation's fade-in, reaction, and fade-out beats.
/// </summary>
[HarmonyPatch(
	typeof(NTransition),
	nameof(NTransition.FadeOut),
	new Type[] { typeof(float), typeof(string), typeof(CancellationToken?) })]
public static class TashkentTransitionDurationPatch
{
	private const string TransitionPath =
		"res://TashkentSpire2/materials/tashkent_transition_mat.tres";

	[HarmonyPrefix]
	public static void Prefix(ref float time, string transitionPath)
	{
		// The selected material already identifies the local player's character,
		// including multiplayer and loaded runs. Do not introduce synchronized state.
		if (transitionPath != TransitionPath)
			return;

		// At the official 0.8 s duration, allow about 1.15 s for the entrance
		// and 1.35 s for the reaction and fade-out.
		// Preserve zero/invalid durations and let NTransition retain cancellation,
		// Instant mode, input blocking, and final black-frame ownership.
		if (time > 0f && float.IsFinite(time) && time <= float.MaxValue / 3.125f)
			time *= 3.125f;
	}
}
