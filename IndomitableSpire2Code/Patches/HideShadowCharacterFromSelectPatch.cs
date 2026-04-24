using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Helpers;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

// [HarmonyPatch(typeof(NCharacterSelectScreen), "InitCharacterButtons")]
// public static class HideShadowCharacterFromSelectPatch
// {
//     [HarmonyPrefix]
//     public static bool Prefix(NCharacterSelectScreen __instance)
//     {
//         // 自己重写一遍这个方法，跳过影子角色！
//         foreach (CharacterModel allCharacter in ModelDb.AllCharacters)
//         {
//             // 【核心】：如果是影子角色，直接跳过，不生成按钮！
//             if (allCharacter.Id.Entry == "Indomitable_Maid") continue;
//
//             // 照抄原版的生成逻辑...
//             var child = __instance._charSelectButtonScene.Instantiate<NCharacterSelectButton>();
//             // ...
//         }
//         // ... (照抄原版的随机按钮生成逻辑) ...
//         _randomCharacterButton = _charSelectButtonScene.Instantiate<NCharacterSelectButton>();
//         _charButtonContainer.AddChildSafely((Node) _randomCharacterButton);
//         _randomCharacterButton.Init((CharacterModel) ModelDb.Character<RandomCharacter>(), (ICharacterSelectButtonDelegate) __instance);
//         UpdateRandomCharacterVisibility();
//         List<NCharacterSelectButton> list = _charButtonContainer.GetChildren().OfType<NCharacterSelectButton>().Where<NCharacterSelectButton>((Func<NCharacterSelectButton, bool>) (c => c.Visible)).ToList<NCharacterSelectButton>();
//         for (int index = 0; index < list.Count; ++index)
//         {
//             list[index].FocusNeighborTop = list[index].GetPath();
//             list[index].FocusNeighborBottom = list[index].GetPath();
//             NCharacterSelectButton ncharacterSelectButton = list[index];
//             NodePath path;
//             if (index <= 0)
//             {
//                 List<NCharacterSelectButton> ncharacterSelectButtonList = list;
//                 path = ncharacterSelectButtonList[ncharacterSelectButtonList.Count - 1].GetPath();
//             }
//             else
//                 path = list[index - 1].GetPath();
//             ncharacterSelectButton.FocusNeighborLeft = path;
//             list[index].FocusNeighborRight = index < list.Count - 1 ? list[index + 1].GetPath() : list[0].GetPath();
//         }
//         return false; // 返回 false 拦截原版方法
//     }
// }

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
public static class HideSkinFromCardLibraryPatch
{
    // 【关键】：设置高优先级 (Priority.High)，确保我们的代码在 BaseLib 的 AdjustFilterScales 之前运行！
    // 这样我们删除了节点后，BaseLib 就能正确计算剩余节点的排版比例。
    [HarmonyPostfix]
    [HarmonyPriority(Priority.High)]
    public static void Postfix(NCardLibrary __instance)
    {
        // 1. 通过反射获取 NCardLibrary 私有的字典
        var cardPoolFilters = AccessTools.Field(typeof(NCardLibrary), "_cardPoolFilters")
            .GetValue(__instance) as Dictionary<CharacterModel, NCardPoolFilter>;
            
        var poolFilters = AccessTools.Field(typeof(NCardLibrary), "_poolFilters")
            .GetValue(__instance) as Dictionary<NCardPoolFilter, Func<CardModel, bool>>;

        if (cardPoolFilters == null || poolFilters == null) return;

        // 2. 找到所有我们想隐藏的影子角色（例如女仆不挠）
        var skinCharactersToHide = cardPoolFilters.Keys
            .Where(c => c is IndomitableMaidCharacter)
            .ToList();

        // 3. 无情抹杀
        foreach (var character in skinCharactersToHide)
        {
            var filterNode = cardPoolFilters[character];
            
            // 从引擎底层的字典中移除映射，防止后续代码按图索骥报错
            cardPoolFilters.Remove(character);
            poolFilters.Remove(filterNode);
            
            // 安全销毁 UI 节点（BaseLib 甚至来不及排版它，它就灰飞烟灭了）
            filterNode.QueueFreeSafely();
        }
    }
}