using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Saves;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// 各皮肤使用独立的 CharacterModel，以便大厅同步和存档保留外观。
/// 它们在玩法上仍是同一个角色，因此角色进度统一使用默认皮肤的 ModelId。
/// </summary>
[HarmonyPatch]
public static class IndomitableSharedProgressionPatch
{
	private static readonly FieldInfo? CharacterStatsField =
		AccessTools.Field(typeof(ProgressState), "_characterStats");
	private static readonly FieldInfo? LocalPlayerField =
		AccessTools.Field(typeof(NGameOverScreen), "_localPlayer");

	private static readonly ConditionalWeakTable<ProgressState, MigrationMarker> MigratedStates = new();
	private static readonly Lock MigrationLock = new();

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

	[HarmonyPatch(typeof(ProgressState), nameof(ProgressState.ToSerializable))]
	[HarmonyPrefix]
	private static void MigrateBeforeSave(ProgressState __instance) => EnsureExistingStatsAreUnified(__instance);

	/// <summary>
	/// 游戏结算写入徽章时会直接以当前皮肤 ID 索引角色统计字典。
	/// 在这次同步调用期间临时设置别名，让徽章写入共享进度，并兼容修改同一索引的其他补丁。
	/// </summary>
	[HarmonyPatch(typeof(NGameOverScreen), "SaveBadgesToProgress")]
	[HarmonyPrefix]
	private static void AddTemporaryGameOverStatsAlias(
		NGameOverScreen __instance,
		out TemporaryStatsAlias? __state)
	{
		__state = null;
		if (LocalPlayerField?.GetValue(__instance) is not Player player)
		{
			MainFile.Logger.Error("Unable to access the game-over local player; Indomitable skin badges could not be redirected.");
			return;
		}

		var variantId = player.Character.Id;
		if (!IsVariantId(variantId))
			return;

		var progress = SaveManager.Instance.Progress;
		EnsureExistingStatsAreUnified(progress);
		if (CharacterStatsField?.GetValue(progress) is not Dictionary<ModelId, CharacterStats> stats)
		{
			MainFile.Logger.Error("Unable to access ProgressState character stats; Indomitable skin badges could not be redirected.");
			return;
		}

		var sharedId = GetProgressionId(variantId);
		if (!stats.TryGetValue(sharedId, out var sharedStats))
			sharedStats = progress.GetOrCreateCharacterStats(sharedId);

		var hadOriginal = stats.TryGetValue(variantId, out var originalStats);
		stats[variantId] = sharedStats;
		__state = new TemporaryStatsAlias(stats, variantId, hadOriginal, originalStats);
	}

	[HarmonyPatch(typeof(NGameOverScreen), "SaveBadgesToProgress")]
	[HarmonyFinalizer]
	private static Exception? RemoveTemporaryGameOverStatsAlias(
		Exception? __exception,
		TemporaryStatsAlias? __state)
	{
		if (__state is not null)
		{
			if (__state.HadOriginal && __state.OriginalStats is not null)
				__state.Stats[__state.VariantId] = __state.OriginalStats;
			else
				__state.Stats.Remove(__state.VariantId);
		}

		return __exception;
	}

	public static ModelId GetProgressionId(ModelId characterId)
	{
		var defaultId = ModelDb.GetId<IndomitableDefault>();
		return IsVariantId(characterId) ? defaultId : characterId;
	}

	private static bool IsVariantId(ModelId characterId)
	{
		return characterId == ModelDb.GetId<IndomitableMaid>()
		       || characterId == ModelDb.GetId<IndomitableRaceQueen>();
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
				MainFile.Logger.Error("Unable to access ProgressState character stats; Indomitable skin progression could not be unified.");
				return;
			}

			var defaultId = ModelDb.GetId<IndomitableDefault>();
			ModelId[] variantIds =
			[
				ModelDb.GetId<IndomitableMaid>(),
				ModelDb.GetId<IndomitableRaceQueen>()
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
			var targetBadge = target.Badges.FirstOrDefault(badge => badge.Id == sourceBadge.Id
				&& badge.Rarity == sourceBadge.Rarity);
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

	private sealed record TemporaryStatsAlias(
		Dictionary<ModelId, CharacterStats> Stats,
		ModelId VariantId,
		bool HadOriginal,
		CharacterStats? OriginalStats);
}

/// <summary>
/// BaseLib 会为每个注册的角色模型添加统计区块，且这里不读取 HideInCompendium。
/// 所有不挠皮肤共用同一份角色统计，因此只保留一个区块。
/// </summary>
[HarmonyPatch(typeof(NGeneralStatsGrid), nameof(NGeneralStatsGrid.LoadStats))]
[HarmonyAfter("BaseLib")]
public static class IndomitableSharedStatsScreenPatch
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
			MainFile.Logger.Error("Unable to access character stats sections; duplicate Indomitable skin stats could not be hidden.");
			return;
		}

		var foundSharedSection = false;
		var sharedId = ModelDb.GetId<IndomitableDefault>();
		var maidId = ModelDb.GetId<IndomitableMaid>();
		var raceQueenId = ModelDb.GetId<IndomitableRaceQueen>();
		foreach (var child in container.GetChildren().OfType<NCharacterStats>())
		{
			if (child.IsQueuedForDeletion()
			    || CharacterStatsField.GetValue(child) is not CharacterStats stats)
				continue;

			if (stats.Id == maidId || stats.Id == raceQueenId)
			{
				child.QueueFree();
				continue;
			}

			if (stats.Id != sharedId)
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