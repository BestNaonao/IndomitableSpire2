using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Afflictions;

public sealed class TorpedoAffliction : AfflictionModel
{
    public override bool HasExtraCardText => true;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != base.Card) return;
        var owner = base.Card.Owner.Creature;

        await PowerCmd.Apply<TorpedoPower>(owner, 18M, owner, null);
    }
}