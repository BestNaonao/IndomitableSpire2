using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using TashkentSpire2.TashkentSpire2Code.Helpers;

namespace TashkentSpire2.TashkentSpire2Code.Patches;

[HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
internal static class DistanceIntentPreviewPatch
{
    private static bool _reportedFailure;

    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void Postfix(AbstractIntent ____intent, Creature ____owner,
        MegaRichTextLabel ____valueLabel, bool ____isFrozen)
    {
        try
        {
            if (____isFrozen || ____intent is not AttackIntent attack || ____owner == null
                || !GodotObject.IsInstanceValid(____valueLabel) || !____valueLabel.IsVisibleInTree()
                || !DistanceIntentPreview.TryGetDamage(attack, ____owner, out int current, out int projected))
                return;

            var original = new LocString("intents", attack is MultiAttackIntent ? "FORMAT_DAMAGE_MULTI" : "FORMAT_DAMAGE_SINGLE");
            original.Add("Damage", current);
            original.Add("Repeat", attack.Repeats);
            string expected = original.GetFormattedText();
            string damage = current.ToString(LocManager.Instance.CultureInfo);
            if (____valueLabel.Text != expected || !expected.StartsWith(damage, StringComparison.Ordinal))
                return;
            ____valueLabel.Text = expected.Insert(damage.Length,
                $"[font_size=14]({projected.ToString(LocManager.Instance.CultureInfo)})[/font_size]");
        }
        catch (Exception exception)
        {
            if (_reportedFailure)
                return;
            _reportedFailure = true;
            MainFile.Logger.Warn($"Distance intent preview unavailable: {exception.Message}");
        }
    }
}
