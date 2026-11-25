using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Runs;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))]
internal static class TashkentCardLibraryPatch
{
    private sealed record FilterAlias(Dictionary<CharacterModel, NCardPoolFilter> Filters,
        CharacterModel Skin, NCardPoolFilter Filter);

    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    private static void Prefix(IRunState? ____runState,
        Dictionary<CharacterModel, NCardPoolFilter> ____cardPoolFilters, out FilterAlias? __state)
    {
        __state = null;
        if (LocalContext.GetMe(____runState)?.Character is not TashkentCharacter { CurrentSkin: not TashkentSkin.Default } skin
            || ____cardPoolFilters.ContainsKey(skin)
            || !____cardPoolFilters.TryGetValue(ModelDb.Character<TashkentCharacter>(), out var filter))
            return;

        ____cardPoolFilters.Add(skin, filter);
        __state = new FilterAlias(____cardPoolFilters, skin, filter);
    }

    [HarmonyFinalizer]
    private static void Finalizer(FilterAlias? __state)
    {
        if (__state != null && __state.Filters.TryGetValue(__state.Skin, out var current)
            && ReferenceEquals(current, __state.Filter))
            __state.Filters.Remove(__state.Skin);
    }
}
