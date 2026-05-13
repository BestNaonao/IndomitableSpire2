using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using TashkentSpire2.TashkentSpire2Code.Cards.Status;
using TashkentSpire2.TashkentSpire2Code.Scripts;

namespace TashkentSpire2.TashkentSpire2Code.Commands;

public static class GetShellCountcmd
{
    public static async Task<int> Execute(
        PlayerChoiceContext choiceContext, 
        Player player, 
        int maxAmount, 
        bool canExceed)
    {
        var choices = new List<CardModel>();
        var state = player.Creature.CombatState!;
        
        choices.Add(state.CreateCard(ModelDb.Card<Shell0>(), player));
        choices.Add(state.CreateCard(ModelDb.Card<Shell1>(), player));
        choices.Add(state.CreateCard(ModelDb.Card<Shell2>(), player));
        choices.Add(state.CreateCard(ModelDb.Card<Shell3>(), player));
        choices.Add(state.CreateCard(ModelDb.Card<Shell4>(), player));
        choices.Add(state.CreateCard(ModelDb.Card<Shell5>(), player));
        choices.Add(state.CreateCard(ModelDb.Card<Shell6>(), player));

        int maxToSelect = canExceed ? 6 : Math.Min(6, maxAmount);

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(player);
        int finalCount = 0;

        bool isLocalPlayer = LocalContext.IsMe(player) && RunManager.Instance.NetService.Type != NetGameType.Replay;

        if (isLocalPlayer)
        {
            var prompt = new LocString("cards", "Tashkent_SelectShellsPrompt");

            prompt.Add("maxamount", (decimal)maxAmount); 
            prompt.Add("max", (decimal)maxToSelect);

            finalCount = await NPlayerHandTashkent.Show(choices, prompt.GetFormattedText(), maxToSelect);

            RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
                player, choiceId, PlayerChoiceResult.FromIndex(finalCount));
        }
        else
        {
            finalCount = (await RunManager.Instance.PlayerChoiceSynchronizer.WaitForRemoteChoice(player, choiceId)).AsIndex();
        }

        return finalCount;
    }
}