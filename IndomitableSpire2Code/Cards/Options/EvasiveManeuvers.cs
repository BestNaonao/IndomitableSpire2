using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Options;

public sealed class EvasiveManeuvers() : VersatileCardOption(OptionId, durabilityCost: 1)
{
    public const string OptionId = "evasive_maneuvers";
    
    public override IEnumerable<DynamicVar> CreateVars() => [new PowerVar<EvasiveManeuversPower>(2M)];
    
    public override void Upgrade(CardModel source) => source.DynamicVars["EvasiveManeuversPower"].UpgradeValueBy(1M);
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EvasiveManeuversPower>(choiceContext, source.Owner.Creature,
            source.DynamicVars["EvasiveManeuversPower"].BaseValue, source.Owner.Creature, source);
    }
}