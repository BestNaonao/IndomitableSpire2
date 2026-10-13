using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Options;

public sealed class LastGasp() : VersatileCardOption(OptionId)
{
    public const string OptionId = "last_gasp";
    
    public override IEnumerable<DynamicVar> CreateVars() =>
        [new PowerVar<LastGaspPower>(1M), new DurabilityCostVar(3M)];
    
    public override int? GetDurabilityCost(CardModel source) => source.DynamicVars.DurabilityCost().IntValue;
    
    public override void Upgrade(CardModel source) => source.DynamicVars.DurabilityCost().UpgradeValueBy(-1M);
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<LastGaspPower>(choiceContext, source.Owner.Creature,
            source.DynamicVars["LastGaspPower"].BaseValue, source.Owner.Creature, source);
    }
}