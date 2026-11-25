using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
internal static class TashkentNationalSpireCompatibilityPatch
{
    [ThreadStatic] private static bool _preserveNextLookup;
    private static bool _installed;

    [HarmonyPrefix]
    private static void Initialize()
    {
        if (_installed)
            return;

        var bridge = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("NationalSpire.GameBridge", false))
            .FirstOrDefault(type => type != null);
        if (bridge == null)
            return;

        var characters = AccessTools.DeclaredMethod(bridge, "Characters");
        if (characters == null || characters.ReturnType != typeof(List<CharacterModel>))
            return;

        _installed = true;
        var harmony = new Harmony(MainFile.ModId + ".NationalSpire");
        List<MethodBase> patched = [];
        try
        {
            harmony.Patch(characters, postfix: new HarmonyMethod(typeof(TashkentNationalSpireCompatibilityPatch), nameof(AfterCharacters)));
            patched.Add(characters);
            (string Type, string Method)[] lookups =
            [
                ("NationalSpire.Coop.CoopNativeRun", "Character"),
                ("NationalSpire.AvatarAssets", "ResolveCharacter"),
                ("NationalSpire.CareerScreen", "ResolveFailure"),
                ("NationalSpire.CareerScreen", "ArenaCharacterName")
            ];
            foreach (var lookup in lookups)
            {
                var type = bridge.Assembly.GetType(lookup.Type, false);
                MethodInfo? method = type == null ? null : AccessTools.DeclaredMethod(type, lookup.Method);
                if (method != null)
                {
                    harmony.Patch(method,
                        prefix: new HarmonyMethod(typeof(TashkentNationalSpireCompatibilityPatch), nameof(BeforeLookup)),
                        finalizer: new HarmonyMethod(typeof(TashkentNationalSpireCompatibilityPatch), nameof(AfterLookup)));
                    patched.Add(method);
                }
            }
            var rules = bridge.Assembly.GetType("NationalSpire.Coop.CoopRules", false);
            var prepare = rules == null ? null : AccessTools.DeclaredMethod(rules, "Prepare");
            if (prepare != null)
            {
                harmony.Patch(prepare, prefix: new HarmonyMethod(typeof(TashkentNationalSpireCompatibilityPatch), nameof(BeforePrepare)));
                patched.Add(prepare);
            }
        }
        catch (Exception exception)
        {
            foreach (var method in patched)
                harmony.Unpatch(method, HarmonyPatchType.All, harmony.Id);
            MainFile.Logger.Warn("NationalSpire skin lookup compatibility could not be installed: " + exception.Message);
        }
    }

    private static void BeforeLookup(out bool __state)
    {
        __state = _preserveNextLookup;
        _preserveNextLookup = true;
    }

    private static void AfterLookup(bool __state) => _preserveNextLookup = __state;

    private static void BeforePrepare(ref IReadOnlySet<string> characters)
    {
        if (!characters.Contains(ModelDb.GetId<TashkentCharacter>().ToString()))
            return;

        var accepted = new HashSet<string>(characters);
        foreach (var skin in TashkentCharacterCatalogPatch.SkinModels)
            accepted.Add(skin.Id.ToString());
        characters = accepted;
    }

    private static void AfterCharacters(ref List<CharacterModel> __result)
    {
        if (!_preserveNextLookup)
            return;

        _preserveNextLookup = false;
        if (!__result.Any(character => character is TashkentCharacter { CurrentSkin: TashkentSkin.Default }))
            return;

        __result = __result.Concat(TashkentCharacterCatalogPatch.SkinModels).DistinctBy(character => character.Id).ToList();
    }
}
