using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches.Character.Skin;

[HarmonyPatch(typeof(AscensionHelper), nameof(AscensionHelper.GetRotatingCharacterIdForStreak))]
internal static class DreadSkinRotationPatch
{
    // 告诉系统“当前正在为角色轮换获取角色列表”的开关
    [ThreadStatic] internal static bool FilteringRoster;
    
    [HarmonyPrefix]
    private static void Prefix(ref ModelId startingCharacter, out bool? __state)
    {
        // 先保存之前开关的值，再打开开关，以正确处理嵌套
        __state = FilteringRoster;
        FilteringRoster = true;
        // 如果其实角色属于不挠，则将其设置为偏好的皮肤模型，与 DreadSkinRosterPatch 配合实现只占用一个轮换位置。
        if (IndomitableSharedProgressionPatch.GetProgressionId(startingCharacter) == ModelDb.GetId<IndomitableDefault>())
            startingCharacter = SkinPreference.PreferredModel.Id;
    }
    
    // 嵌套调用恢复外层状态。若其他补丁跳过本 Mod 的 Prefix，Finalizer 无需还原。
    [HarmonyFinalizer]
    private static void Finalizer(bool? __state)
    {
        if (__state.HasValue) FilteringRoster = __state.Value;
    }
}

/// <summary>
/// 只在计算获取轮换角色期间对 BaseLib 处理后的 AllCharacters 进行非偏好皮肤模型过滤，不影响后续其他流程的 AllCharacters 的获取。
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
internal static class DreadSkinRosterPatch
{
    // BaseLib 在自己的 Postfix 中追加角色。保证我们拿到的是最终完整的角色列表，然后再对其进行过滤。轮换期间只过滤本模组的皮肤。
    [HarmonyPostfix, HarmonyAfter("BaseLib"), HarmonyPriority(Priority.Last)]
    private static IEnumerable<CharacterModel> Postfix(IEnumerable<CharacterModel> __result) =>
        DreadSkinRotationPatch.FilteringRoster
            ? __result.Where(character => character is not Indomitable { HideFromVanillaCharacterSelect: true })
            : __result;
}