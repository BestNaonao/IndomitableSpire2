using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Saves;
using TashkentSpire2.TashkentSpire2Code.Character;

namespace TashkentSpire2.TashkentSpire2Code.Models;

/// <summary>
/// Displays a profile-scoped, one-time acknowledgement when the local Tashkent
/// player first reaches The Insatiable.
/// </summary>
public sealed class TheInsatiableFirstEncounterPrompt : CustomSingletonModel
{
    private const string PopupSaveKey = "tashkentspire2_the_insatiable_first_encounter";
    private const string PopupLocKey = "TASHKENTSPIRE2-THE_INSATIABLE_FIRST_ENCOUNTER_PROMPT";

    public TheInsatiableFirstEncounterPrompt() : base(HookType.Combat)
    {
    }

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player.Character is not TashkentCharacter
            || player.PlayerCombatState?.TurnNumber != 1
            || combatState.Encounter is not TheInsatiableBoss)
        {
            return;
        }

        // Every peer enters the same official hook action, even when the owning
        // profile has seen the popup. Only the owning peer touches profile data
        // or UI; remote peers wait for the synchronized action to resume.
        await choiceContext.SignalPlayerChoiceBegun(player, PlayerChoiceOptions.None);
        try
        {
            if (LocalContext.IsMe(player) && !SaveManager.Instance.SeenPopup(PopupSaveKey))
            {
                await ShowPopup();
            }
        }
        finally
        {
            await choiceContext.SignalPlayerChoiceEnded();
        }
    }

    private static async Task ShowPopup()
    {
        NModalContainer? modalContainer = NModalContainer.Instance;
        NGenericPopup? popup = NGenericPopup.Create();
        if (modalContainer == null || popup == null)
        {
            MainFile.Logger.Warn("Could not show The Insatiable first-encounter popup because modal UI is unavailable.");
            return;
        }

        // NModalContainer accepts only one modal. Queue behind any official FTUE
        // that happens to be open instead of adding an unreachable popup.
        while (modalContainer.OpenModal != null)
        {
            await modalContainer.AwaitProcessFrame();
        }

        SaveManager.Instance.MarkFtueAsComplete(PopupSaveKey);
        modalContainer.Add(popup);
        await popup.WaitForConfirmation(
            body: new LocString("settings_ui", $"{PopupLocKey}.body"),
            header: new LocString("settings_ui", $"{PopupLocKey}.header"),
            noButton: null,
            yesButton: new LocString("main_menu_ui", "GENERIC_POPUP.confirm"));
    }
}
