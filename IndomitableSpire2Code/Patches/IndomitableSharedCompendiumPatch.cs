using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

/// <summary>
/// ModelDb 为每个角色模型公开一个角色池的记录，但是不移除重复的角色池实例。
/// 不挠的皮肤模型特意设计为共享相同的角色池，所以在药水池和遗物池进入百科之前，需要对它们进行标准化。
/// </summary>
public static class IndomitableSharedCompendiumPatch
{
	[ThreadStatic]
	private static bool _isLoadingStarterRelicCompendium;
	
	[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacterPotionPools), MethodType.Getter)]
	private static class DistinctPotionPools
	{
		[HarmonyPostfix]
		private static void Postfix(ref IEnumerable<PotionPoolModel> __result)
		{
			__result = KeepSingleIndomitablePool(__result, ModelDb.GetId<IndomitablePotionPool>());
		}
	}
	
	[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacterRelicPools), MethodType.Getter)]
	private static class DistinctRelicPools
	{
		[HarmonyPostfix]
		private static void Postfix(ref IEnumerable<RelicPoolModel> __result)
		{
			__result = KeepSingleIndomitablePool(__result, ModelDb.GetId<IndomitableRelicPool>());
		}
	}
	
	private static IEnumerable<TPool> KeepSingleIndomitablePool<TPool>(
		IEnumerable<TPool> pools,
		ModelId indomitablePoolId)
		where TPool : AbstractModel
	{
		var foundIndomitablePool = false;
		foreach (var pool in pools)
		{
			if (pool.Id != indomitablePoolId)
			{
				yield return pool;
				continue;
			}
			
			if (foundIndomitablePool)
				continue;
			
			foundIndomitablePool = true;
			yield return pool;
		}
	}
	
	/// <summary>
	/// 初始遗物是从 AllCharacters 中收集的，而不是 AllCharacterRelicPools，因此必须在构建同步收集初期，才能隐藏的附属皮肤模型。
	/// 角色的 getter 方法在游戏运行时和角色选择时保持不变。
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
	
	[HarmonyPatch(typeof(Indomitable), nameof(Indomitable.StartingRelics), MethodType.Getter)]
	private static class HideSkinStarterRelicsFromCompendium
	{
		[HarmonyPostfix]
		private static void Postfix(Indomitable __instance, ref IReadOnlyList<RelicModel> __result)
		{
			if (_isLoadingStarterRelicCompendium && __instance.CurrentSkin != IndomitableSkin.Default)
				__result = Array.Empty<RelicModel>();
		}
	}
}