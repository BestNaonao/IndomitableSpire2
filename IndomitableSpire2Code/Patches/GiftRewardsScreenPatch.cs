using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Rewards;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Rewards;

namespace IndomitableSpire2.IndomitableSpire2Code.Patches;

[HarmonyPatch(typeof(NRewardsScreen), nameof(NRewardsScreen.ShowScreen))]
public static class GiftRewardsScreenPatch
{
    [HarmonyPostfix]
    private static void Postfix(RewardsSet set, NRewardsScreen __result)
    {
        if (!set.Rewards.Any(reward => reward is GiftRelicReward)) return;
        if (__result.IsNodeReady()) SetHeader();
        else __result.Ready += SetHeader;
        return;
        
        void SetHeader() => __result.GetNode<MegaLabel>("%HeaderLabel").SetTextAutoSize(
            new LocString("gameplay_ui", "INDOMITABLESPIRE2-GIFT_REWARD_HEADER").GetFormattedText());
    }
}