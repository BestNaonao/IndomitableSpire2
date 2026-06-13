using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(TheBomb), "OnPlay")]
public class TheBombReplacementPatch
{
    [HarmonyPrefix]
    static bool ReplaceWithTorpedo(TheBomb __instance, PlayerChoiceContext choiceContext, ref Task __result)
    {
        __result = ApplyTorpedoAsync(__instance, choiceContext);
        
        return false;
    }

    private static async Task ApplyTorpedoAsync(TheBomb card, PlayerChoiceContext choiceContext)
    {
        decimal damage = card.DynamicVars["BombDamage"].BaseValue;

        var power = await PowerCmd.Apply<TorpedoPower>(choiceContext, card.Owner.Creature, damage, card.Owner.Creature, card);
        
        if (power is TorpedoPower torpedo) {
            torpedo.SetIsTheBomb(true);
        }
    }
}