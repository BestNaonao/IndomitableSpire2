using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.PopulateLocKeys))]
internal static class TashkentCharacterCatalogPatch
{
    [ThreadStatic] internal static bool IncludeSkinModels;

    internal static IEnumerable<CharacterModel> SkinModels =>
    [
        ModelDb.Character<TashkentMu>(),
        ModelDb.Character<TashkentVariantTwo>(),
        ModelDb.Character<TashkentVariantThree>(),
        ModelDb.Character<TashkentVariantFour>()
    ];

    [HarmonyPrefix, HarmonyPriority(Priority.First)]
    private static void Prefix(out bool __state)
    {
        __state = IncludeSkinModels;
        IncludeSkinModels = true;
    }

    [HarmonyFinalizer]
    private static void Finalizer(bool __state) => IncludeSkinModels = __state;
}
