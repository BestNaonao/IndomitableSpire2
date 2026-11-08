using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using TashkentSpire2.TashkentSpire2Code.Character;
using TashkentSpire2.TashkentSpire2Code.Nodes.Vfx;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

/// <summary>Line-bound, local presentation only; never advances dialogue or changes combat state.</summary>
[HarmonyPatch(typeof(TheArchitect), "ShowSpeechBubble")]
internal static class TashkentArchitectDialoguePatch
{
    private static readonly ConditionalWeakTable<TheArchitect, HashSet<string>> Played = new();

    internal static int ShotCount(string? lineKey, string characterEntry)
    {
        string prefix = $"THE_ARCHITECT.talk.{characterEntry}.";
        if (lineKey == prefix + "0-4r.char") return 1;
        return lineKey == prefix + "1-5r.char" ? 3 : 0;
    }

    [HarmonyPostfix]
    private static void Postfix(TheArchitect __instance, AncientDialogueLine line)
    {
        var owner = __instance.Owner;
        if (TestMode.IsOn || owner == null || !LocalContext.IsMe(owner) ||
            owner.Character is not TashkentCharacter character ||
            line.Speaker != AncientDialogueSpeaker.Character || line.LineText == null)
            return;
        string key = line.LineText.LocEntryKey;
        int shots = ShotCount(key, character.Id.Entry);
        if (shots == 0 || NCombatRoom.Instance is not { } room)
            return;
        if (Played.GetOrCreateValue(__instance).Contains(key)) return;
        if (NTashkentArchitectShotVfx.Play(room, owner.Creature, shots))
            Played.GetOrCreateValue(__instance).Add(key);
    }
}

// A fast click-through must not leave a delayed Hit trigger interrupting the
// Architect's vanilla finishing attack or its existing win-run sequence.
[HarmonyPatch(typeof(TheArchitect), "WinRun")]
internal static class TashkentArchitectDialogueFinishPatch
{
    [HarmonyPrefix]
    private static void Prefix(TheArchitect __instance)
    {
        if (__instance.Owner?.Character is TashkentCharacter && LocalContext.IsMe(__instance.Owner))
            NTashkentArchitectShotVfx.StopForRoom(NCombatRoom.Instance);
    }
}
