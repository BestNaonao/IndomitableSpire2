using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Cards.Options;

public sealed class ScheduledMaintenance() : VersatileCardOption(OptionId, durabilityCost: 2)
{
    public const string OptionId = "scheduled_maintenance";
    
    public override IEnumerable<DynamicVar> CreateVars() => [new PowerVar<ScheduledMaintenancePower>(3M)];
    
    public override void Upgrade(CardModel source) => source.DynamicVars["ScheduledMaintenancePower"].UpgradeValueBy(2M);
    
    public override async Task Execute(CardModel source, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = source.Owner;
        await PowerCmd.Apply<ScheduledMaintenancePower>(choiceContext, owner.Creature,
            source.DynamicVars["ScheduledMaintenancePower"].BaseValue, owner.Creature, source);
    }
}