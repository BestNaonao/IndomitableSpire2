using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
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
	[ThreadStatic]
	private static bool _isLoadingStarterRelicCompendium;

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
	/// so suppress the three hidden skin models only while that synchronous collection is
	/// being built. The character getter remains unchanged for runs and character select.
	/// </summary>
	[HarmonyPatch(typeof(NRelicCollectionCategory), nameof(NRelicCollectionCategory.LoadRelics))]
	private static class DistinctStarterRelics
	{
		[HarmonyPrefix]
		private static void Prefix(RelicRarity relicRarity, out bool __state)
		{
			__state = _isLoadingStarterRelicCompendium;
			if (relicRarity == RelicRarity.Starter)
				_isLoadingStarterRelicCompendium = true;
		}

		[HarmonyFinalizer]
		private static Exception? Finalizer(Exception? __exception, bool __state)
		{
			_isLoadingStarterRelicCompendium = __state;
			return __exception;
		}
	}

	[HarmonyPatch(typeof(TashkentCharacter), nameof(TashkentCharacter.StartingRelics), MethodType.Getter)]
	private static class HideSkinStarterRelicsFromCompendium
	{
		[HarmonyPostfix]
		private static void Postfix(TashkentCharacter __instance, ref IReadOnlyList<RelicModel> __result)
		{
			if (_isLoadingStarterRelicCompendium && __instance.CurrentSkin != TashkentSkin.Default)
				__result = Array.Empty<RelicModel>();
		}
	}
}
