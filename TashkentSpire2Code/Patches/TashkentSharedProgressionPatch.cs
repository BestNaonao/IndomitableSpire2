using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Saves;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

/// <summary>
/// The selectable Spine variants are separate CharacterModel instances so the selected
/// appearance survives lobby synchronization and run saves. They are still one gameplay
/// character, so all per-character progression must use TashkentCharacter's ModelId.
/// </summary>
[HarmonyPatch]
public static class TashkentSharedProgressionPatch
{
	private static readonly FieldInfo? CharacterStatsField =
		AccessTools.Field(typeof(ProgressState), "_characterStats");

	private static readonly ConditionalWeakTable<ProgressState, MigrationMarker> MigratedStates = new();
	private static readonly object MigrationLock = new();

	[HarmonyPatch(typeof(ProgressState), nameof(ProgressState.GetOrCreateCharacterStats))]
	[HarmonyPrefix]
	private static void RedirectMutableStats(ProgressState __instance, ref ModelId characterId)
	{
		EnsureExistingStatsAreUnified(__instance);
		characterId = GetProgressionId(characterId);
	}

	[HarmonyPatch(typeof(ProgressState), nameof(ProgressState.GetStatsForCharacter))]
	[HarmonyPrefix]
	private static void RedirectStatsLookup(ProgressState __instance, ref ModelId characterId)
	{
		EnsureExistingStatsAreUnified(__instance);
		characterId = GetProgressionId(characterId);
	}

	/// <summary>
	/// The game-over badge writer is the one progression call site that indexes
	/// ProgressState.CharacterStats directly instead of using one of the methods above.
	/// Redirect that index without adding duplicate dictionary entries to the save.
	/// </summary>
	[HarmonyPatch(typeof(NGameOverScreen), "SaveBadgesToProgress")]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> RedirectGameOverBadgeStats(
		IEnumerable<CodeInstruction> instructions)
	{
		var dictionaryIndexer = AccessTools.PropertyGetter(
			typeof(IReadOnlyDictionary<ModelId, CharacterStats>),
			"Item");
		var sharedIndexer = AccessTools.DeclaredMethod(
			typeof(TashkentSharedProgressionPatch),
			nameof(GetSharedCharacterStats));
		var replaced = false;

		foreach (var instruction in instructions)
		{
			if (instruction.Calls(dictionaryIndexer))
			{
				replaced = true;
				yield return new CodeInstruction(OpCodes.Call, sharedIndexer);
				continue;
			}

			yield return instruction;
		}

		if (!replaced)
			MainFile.Logger.Error("Unable to patch the game-over character badge lookup.");
	}

	private static CharacterStats GetSharedCharacterStats(
		IReadOnlyDictionary<ModelId, CharacterStats> stats,
		ModelId characterId)
	{
		return stats[GetProgressionId(characterId)];
	}

	public static ModelId GetProgressionId(ModelId characterId)
	{
		var defaultId = ModelDb.GetId<TashkentCharacter>();
		return IsVariantId(characterId) ? defaultId : characterId;
	}

	private static bool IsVariantId(ModelId characterId)
	{
		return characterId == ModelDb.GetId<TashkentVariantTwo>()
		       || characterId == ModelDb.GetId<TashkentVariantThree>()
		       || characterId == ModelDb.GetId<TashkentVariantFour>();
	}

	private static void EnsureExistingStatsAreUnified(ProgressState progress)
	{
		if (MigratedStates.TryGetValue(progress, out _))
			return;

		lock (MigrationLock)
		{
			if (MigratedStates.TryGetValue(progress, out _))
				return;

			if (CharacterStatsField?.GetValue(progress) is not Dictionary<ModelId, CharacterStats> stats)
			{
				MainFile.Logger.Error("Unable to access ProgressState character stats; Tashkent skin progression could not be unified.");
				return;
			}

			var defaultId = ModelDb.GetId<TashkentCharacter>();
			ModelId[] variantIds =
			[
				ModelDb.GetId<TashkentVariantTwo>(),
				ModelDb.GetId<TashkentVariantThree>(),
				ModelDb.GetId<TashkentVariantFour>()
			];

			var existingVariantStats = variantIds
				.Select(id => stats.GetValueOrDefault(id))
				.OfType<CharacterStats>()
				.ToArray();

			if (existingVariantStats.Length > 0)
			{
				if (!stats.TryGetValue(defaultId, out var sharedStats))
				{
					sharedStats = new CharacterStats { Id = defaultId };
					stats.Add(defaultId, sharedStats);
				}

				foreach (var variantStats in existingVariantStats)
					MergeStats(sharedStats, variantStats);

				sharedStats.PreferredAscension = Math.Min(sharedStats.PreferredAscension, sharedStats.MaxAscension);
				foreach (var variantId in variantIds)
					stats.Remove(variantId);
			}

			MigratedStates.Add(progress, new MigrationMarker());
		}
	}

	private static void MergeStats(CharacterStats target, CharacterStats source)
	{
		target.MaxAscension = Math.Max(target.MaxAscension, source.MaxAscension);
		target.PreferredAscension = Math.Max(target.PreferredAscension, source.PreferredAscension);
		target.TotalWins = SaturatingAdd(target.TotalWins, source.TotalWins);
		target.TotalLosses = SaturatingAdd(target.TotalLosses, source.TotalLosses);
		target.Playtime = SaturatingAdd(target.Playtime, source.Playtime);
		target.BestWinStreak = Math.Max(target.BestWinStreak, source.BestWinStreak);
		target.CurrentWinStreak = Math.Max(target.CurrentWinStreak, source.CurrentWinStreak);
		target.FastestWinTime = MergeFastestTime(target.FastestWinTime, source.FastestWinTime);

		foreach (var sourceBadge in source.Badges)
		{
			var targetBadge = target.Badges.FirstOrDefault(badge => badge.Id == sourceBadge.Id);
			if (targetBadge is null)
			{
				target.Badges.Add(new BadgeStats
				{
					Id = sourceBadge.Id,
					Count = sourceBadge.Count,
					Rarity = sourceBadge.Rarity
				});
			}
			else
			{
				targetBadge.Count = SaturatingAdd(targetBadge.Count, sourceBadge.Count);
				if ((int)sourceBadge.Rarity > (int)targetBadge.Rarity)
					targetBadge.Rarity = sourceBadge.Rarity;
			}
		}
	}

	private static int SaturatingAdd(int left, int right)
	{
		var total = (long)left + right;
		return total >= int.MaxValue ? int.MaxValue : (int)total;
	}

	private static long SaturatingAdd(long left, long right)
	{
		if (right > 0 && left > long.MaxValue - right)
			return long.MaxValue;

		return left + right;
	}

	private static long MergeFastestTime(long left, long right)
	{
		if (left < 0)
			return right;
		if (right < 0)
			return left;
		return Math.Min(left, right);
	}

	private sealed class MigrationMarker
	{
	}
}

/// <summary>
/// BaseLib appends one stats-screen section per registered CharacterModel and currently
/// does not honor HideInCompendium there. Since all Tashkent skins now resolve to the
/// same CharacterStats instance, retain only one section for that shared entry.
/// </summary>
[HarmonyPatch(typeof(NGeneralStatsGrid), nameof(NGeneralStatsGrid.LoadStats))]
[HarmonyAfter("BaseLib")]
public static class TashkentSharedStatsScreenPatch
{
	private static readonly FieldInfo? CharacterStatsField =
		AccessTools.Field(typeof(NCharacterStats), "_characterStats");
	private static readonly FieldInfo? CharacterStatContainerField =
		AccessTools.Field(typeof(NGeneralStatsGrid), "_characterStatContainer");

	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	private static void RemoveDuplicateSkinSections(NGeneralStatsGrid __instance)
	{
		if (CharacterStatsField is null
		    || CharacterStatContainerField?.GetValue(__instance) is not Godot.Node container)
		{
			MainFile.Logger.Error("Unable to access character stats sections; duplicate Tashkent skin stats could not be hidden.");
			return;
		}

		var foundSharedSection = false;
		var sharedId = ModelDb.GetId<TashkentCharacter>();
		foreach (var child in container.GetChildren().OfType<NCharacterStats>())
		{
			if (CharacterStatsField.GetValue(child) is not CharacterStats stats || stats.Id != sharedId)
				continue;

			if (!foundSharedSection)
			{
				foundSharedSection = true;
				continue;
			}

			child.QueueFree();
		}
	}
}
