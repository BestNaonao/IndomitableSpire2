using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

/// <summary>
/// ModelDb exposes one character pool entry per CharacterModel and does not remove
/// duplicate pool instances. Tashkent's skin models deliberately share the same pools,
/// so normalize these enumerations before the potion and relic compendia consume them.
/// </summary>
public static class TashkentSharedCompendiumPatch
{
	[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacterPotionPools), MethodType.Getter)]
	private static class DistinctPotionPools
	{
		[HarmonyPostfix]
		private static void Postfix(ref IEnumerable<PotionPoolModel> __result)
		{
			__result = KeepSingleTashkentPool(__result, ModelDb.GetId<TashkentPotionPool>());
		}
	}

	[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacterRelicPools), MethodType.Getter)]
	private static class DistinctRelicPools
	{
		[HarmonyPostfix]
		private static void Postfix(ref IEnumerable<RelicPoolModel> __result)
		{
			__result = KeepSingleTashkentPool(__result, ModelDb.GetId<TashkentRelicPool>());
		}
	}

	private static IEnumerable<TPool> KeepSingleTashkentPool<TPool>(
		IEnumerable<TPool> pools,
		ModelId tashkentPoolId)
		where TPool : AbstractModel
	{
		var foundTashkentPool = false;
		foreach (var pool in pools)
		{
			if (pool.Id != tashkentPoolId)
			{
				yield return pool;
				continue;
			}

			if (foundTashkentPool)
				continue;

			foundTashkentPool = true;
			yield return pool;
		}
	}

	/// <summary>
	/// Starter relics are collected from AllCharacters instead of AllCharacterRelicPools,
	/// so the three hidden skin models must also be filtered at this call site.
	/// </summary>
	[HarmonyPatch(typeof(NRelicCollectionCategory), nameof(NRelicCollectionCategory.LoadRelics))]
	private static class DistinctStarterRelics
	{
		private static readonly MethodInfo AllCharactersGetter =
			AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters));

		private static readonly MethodInfo CompendiumCharactersMethod =
			AccessTools.DeclaredMethod(typeof(DistinctStarterRelics), nameof(GetCompendiumCharacters));

		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var replaced = false;
			foreach (var instruction in instructions)
			{
				if (instruction.Calls(AllCharactersGetter))
				{
					replaced = true;
					yield return new CodeInstruction(OpCodes.Call, CompendiumCharactersMethod);
					continue;
				}

				yield return instruction;
			}

			if (!replaced)
				MainFile.Logger.Error("Unable to patch the relic compendium's character enumeration.");
		}

		private static IEnumerable<CharacterModel> GetCompendiumCharacters()
		{
			return ModelDb.AllCharacters.Where(character =>
				character is not TashkentCharacter tashkent || tashkent.CurrentSkin == TashkentSkin.Default);
		}
	}
}
