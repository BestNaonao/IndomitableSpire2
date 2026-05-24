using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards.Status;

namespace TashkentSpire2.TashkentSpire2Code.Deprecated;

// public static class GetShellCountcmdOld
// {
//     public static async Task<int> Execute(
//         PlayerChoiceContext choiceContext, 
//         Player player, 
//         int maxAmount, 
//         bool canExceed)
//     {
//         if (!canExceed && maxAmount == 0) return 0;
//         
//         var shellModel = ModelDb.Card<Shell>();
//         var choices = new List<CardModel>(); 
//     
//         for (int i = 0; i < 6; i++)
//         {
//             CardModel shellInstance = player.Creature.CombatState!.CreateCard(shellModel, player);
//             choices.Add(shellInstance);
//         }
//     
//         int maxToSelect = canExceed ? 6 : Math.Min(6, maxAmount);
//     
//         var prompt = new LocString("cards", "Tashkent_SelectShellsPrompt");
//         prompt.Add("maxamount", (decimal)maxAmount);
//         prompt.Add("max", (decimal)maxToSelect); 
//     
//         var prefs = new CardSelectorPrefs(
//             prompt, 
//             0, 
//             maxToSelect
//         );
//         
//         var selected = (await CardSelectCmd.FromSimpleGrid(
//             choiceContext,
//             choices,
//             player,
//             prefs
//         )).ToList();
//
//         return selected.Count;
//     }
// }