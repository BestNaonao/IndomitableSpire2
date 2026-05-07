using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Enchantment;

public sealed class SolidarityEnchantment : CustomEnchantmentModel
{
    public override bool ShowAmount => false;

    public override bool HasExtraCardText => true;

    protected override string? CustomIconPath =>
        "res://TashkentSpire2/images/enchantment/oath_enchantment.png";

    private static bool _isTriggering = false;

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (_isTriggering || cardPlay == null)
            return;

        var owner = cardPlay.Card.Owner;
        if (owner == null)
            return;

        var currentCard = cardPlay?.Card;

        try
        {
            _isTriggering = true;

            var solidarityCards = owner.PlayerCombatState?.AllCards
                .Where(c => c != currentCard && c.Pile != null && c.Pile.Type != PileType.Play && c.Enchantment is SolidarityEnchantment)
                .Distinct()
                .ToList();
            if (solidarityCards != null)
            {
                foreach (var card in solidarityCards)
                {
                    await CardCmd.AutoPlay(choiceContext, card, null);
                }
            }
        }
        finally
        {
            _isTriggering = false;
        }
    }
}