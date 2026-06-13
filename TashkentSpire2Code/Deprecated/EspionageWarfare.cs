using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Cards;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Deprecated;

// public sealed class EspionageWarfare() : TashkentCard(1, CardType.Skill, CardRarity.Ancient, TargetType.AnyAlly)
// {
//
//     
//     protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
// {
//     Creature? target = cardPlay.Target;
//     Creature? player = base.Owner?.Creature;
//
//     if (target == null || player == null) return;
//     
//     List<PowerModel> availableBuffs = target.Powers
//         .Where(p => p.TypeForCurrentAmount == PowerType.Buff && p.Amount > 0)
//         .ToList();
//
//     if (availableBuffs.Count == 0) return;
//
//     var rng = base.Owner?.RunState.Rng.Niche;
//     if (rng == null) return;
//     PowerModel? stolenPower = rng.NextItem(availableBuffs);
//     if (stolenPower == null) return;
//
//     int stealAmount = stolenPower.Amount; 
//
//     int targetNewAmount = stolenPower.Amount - stealAmount;
//     stolenPower.SetAmount(targetNewAmount);
//
//     if (stolenPower.ShouldRemoveDueToAmount())
//     {
//         stolenPower.RemoveInternal();
//     }
//
//     PowerModel? existingPlayerPower = player.Powers.FirstOrDefault(p => p.Id == stolenPower.Id);
//
//     if (existingPlayerPower != null)
//     {
//         existingPlayerPower.SetAmount(existingPlayerPower.Amount + stealAmount);
//     }
//     else
//     {
//         PowerModel? canonicalTemplate = ModelDb.GetById<PowerModel>(stolenPower.Id);
//         if (canonicalTemplate != null)
//         {
//             PowerModel newPlayerPower = canonicalTemplate.ToMutable(0);
//
//             newPlayerPower.ApplyInternal(player, (decimal)stealAmount);
//         }
//     }
//
//     player.Powers.FirstOrDefault(p => p.Id == stolenPower.Id)?.StartPulsing();
//
//     await Task.CompletedTask;
// }
//
//     protected override void OnUpgrade()
//     {
//         base.EnergyCost.UpgradeBy(-1);
//     }
// }