using System.Reflection;
using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.StatsScreen;
using MegaCrit.Sts2.Core.Saves;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

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
			if (child.IsQueuedForDeletion() || CharacterStatsField.GetValue(child) is not CharacterStats stats)
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