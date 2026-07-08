using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Multiplay;

// public sealed class RussianRoulette() : AmmunitionCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
// {
//     public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
//
//     protected override IEnumerable<DynamicVar> CanonicalVars => [
//         new DamageVar(10M, ValueProp.Unpowered),
//         new AmmunitionDynamicVar(6M),
//         new LoadDynamicVar(6M),
//         new AmmuMaxDynamicVar(6M),
//         new PowerVar<VigorPower>(2M),
//         new PowerVar<RegenPower>(1M)
//     ];
//     
//     protected override IEnumerable<IHoverTip> ExtraHoverTips => [
//         HoverTipFactory.FromCard<Shame>(),
//         HoverTipFactory.FromPower<VigorPower>(),
//         HoverTipFactory.FromPower<RegenPower>()
//     ];
//     
//     protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
//     {
//         
//     }
//     
//     protected override void OnUpgrade()
//     {
//         DynamicVars["VigorPower"].UpgradeValueBy(1M);
//     }
// }