using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Cards.Rare;

public sealed class TorpedoTorpedoTorpedo() : TashkentCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new TorpedoDynamicVar(18M),
        new CalculationBaseVar(0M),
        new CalculationExtraVar(1M),
        new CalculatedVar("CalculatedTorpedo").WithMultiplier((CardModel card, Creature? _) =>
        {
            var owner = card.Owner?.Creature;
            if (owner == null)
                return 0;
            return owner.Powers.OfType<TorpedoPower>().Count();
        }),
        new CalculatedVar("CalculatedBuff").WithMultiplier((CardModel card, Creature? _) =>
        {
            var owner = card.Owner?.Creature;
            if (owner == null)
                return 0;
            return owner.Powers.Count(p => p.TypeForCurrentAmount == PowerType.Buff);
        })
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
        int torpedoCount = base.IsUpgraded ? 
            (int)((CalculatedVar)base.DynamicVars["CalculatedBuff"]).Calculate(cardPlay.Target) : 
            (int)((CalculatedVar)base.DynamicVars["CalculatedTorpedo"]).Calculate(cardPlay.Target);
        for (int i = 0; i < torpedoCount; i++)
        {
            await PowerCmd.Apply<TorpedoPower>(choiceContext, base.Owner.Creature, DynamicVars["TashkentSpire2-Torpedo"].BaseValue, base.Owner.Creature, this);
        }
    }
}