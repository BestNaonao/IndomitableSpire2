using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Options;

public sealed class Dynorphin() : VersatileCardOption(OptionId, durabilityCost: 1)
{
    public const string OptionId = "dynorphin";
    
    public override IEnumerable<DynamicVar> CreateVars() => [new PowerVar<DynorphinPower>(2M)];
    
    public override void Upgrade(CardModel source) => source.DynamicVars["DynorphinPower"].UpgradeValueBy(1M);
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DynorphinPower>(choiceContext, source.Owner.Creature,
            source.DynamicVars["DynorphinPower"].BaseValue, source.Owner.Creature, source);
    }
}