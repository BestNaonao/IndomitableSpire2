using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

/// <summary>Start a visual-only victory cue after the normal reward flow has been dispatched.</summary>
[HarmonyPatch(typeof(NCombatUi), "OnCombatWon")]
public static class TashkentVictoryAnimationPatch
{
	[HarmonyPostfix]
	public static void Postfix(NCombatUi __instance)
	{
		if (TestMode.IsOn)
			return;

		NCombatRoom? room = NCombatRoom.Instance;
		if (room == null || room.Ui != __instance)
			return;

		// Include remote players as well; no local-player shortcut, gameplay RNG,
		// animation-completion task, timer or extra network message is involved.
		foreach (NCreature node in room.CreatureNodes)
		{
			if (!GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion() || !node.IsInsideTree()
			    || !node.Entity.IsAlive || node.Entity.Player?.Character is not TashkentCharacter)
				continue;

			var spine = node.Visuals.SpineBody;
			if (spine == null || !spine.HasAnimation(TashkentCharacter.VictoryAnimationName))
				continue;

			node.SetAnimationTrigger(TashkentCharacter.VictoryAnimationTrigger);
		}
	}
}
