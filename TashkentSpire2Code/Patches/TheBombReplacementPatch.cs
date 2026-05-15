using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Cards;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(TheBomb), "OnPlay")]
public class TheBombReplacementPatch
{
    [HarmonyPrefix]
    static bool ReplaceWithTorpedo(TheBomb __instance, ref Task __result)
    {
        __result = ApplyTorpedoAsync(__instance);
        
        return false;
    }

    private static async Task ApplyTorpedoAsync(TheBomb card)
    {
        decimal damage = card.DynamicVars["BombDamage"].BaseValue;

        var power = await PowerCmd.Apply<TorpedoPower>(card.Owner.Creature, damage, card.Owner.Creature, card);
        
        if (power is TorpedoPower torpedo) {
            torpedo.SetIsTheBomb(true);
        }
    }
}