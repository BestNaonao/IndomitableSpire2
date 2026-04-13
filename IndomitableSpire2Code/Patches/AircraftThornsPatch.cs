using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(ThornsPower), nameof(ThornsPower.BeforeDamageReceived))]
public static class AircraftThornsPatch
{
    [HarmonyPrefix]
    public static bool Prefix(CardModel? cardSource)
    {
        // 如果伤害来源是舰载机，直接拦截荆棘的反伤逻辑，否则正常反伤
        return cardSource is not CarrierAircraftCard;
    }
}