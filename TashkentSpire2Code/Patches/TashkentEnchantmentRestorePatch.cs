using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using TashkentSpire2.TashkentSpire2Code.Enchantment;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch]
internal static class TashkentEnchantmentRestorePatch
{
    [ThreadStatic]
    private static Stack<SerializableCard>? _restoringCards;

    private static readonly AccessTools.FieldRef<CardModel, EnchantmentModel?> PrimaryEnchantment =
        AccessTools.FieldRefAccess<CardModel, EnchantmentModel?>("<Enchantment>k__BackingField");

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void BeforeRestore(SerializableCard save, out bool __state)
    {
        (_restoringCards ??= new Stack<SerializableCard>()).Push(save);
        __state = true;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable))]
    [HarmonyFinalizer]
    private static void AfterRestore(bool __state)
    {
        if (__state)
            _restoringCards!.Pop();
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.EnchantInternal))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyAfter("MultiEnchantmentMod")]
    private static void EnsureRestoredEnchantment(CardModel __instance, EnchantmentModel enchantment, decimal amount)
    {
        if (__instance.Enchantment != null || _restoringCards == null || _restoringCards.Count == 0 ||
            enchantment is not (FermentEnchantment or CommissarEnchantment or EndeavourEnchantment or
                SolidarityEnchantment or OathEnchantment))
            return;

        SerializableCard save = _restoringCards.Peek();
        if (save.Enchantment?.Id != enchantment.Id || save.Id != __instance.Id)
            return;

        if (enchantment.HasCard)
        {
            if (enchantment.Card != __instance)
                return;
        }
        else
        {
            enchantment.ApplyInternal(__instance, amount);
        }

        PrimaryEnchantment(__instance) = enchantment;
        MainFile.Logger.Warn($"Restored missing primary Tashkent enchantment {enchantment.Id} on {__instance.Id} during card deserialization.");
    }
}
