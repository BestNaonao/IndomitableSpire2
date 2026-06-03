using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using TashkentSpire2.TashkentSpire2Code.Cards.Token;

namespace TashkentSpire2.TashkentSpire2Code.Enchantment;

public sealed class FermentEnchantment : CustomEnchantmentModel
{
    public override bool ShowAmount => false;

    public override bool HasExtraCardText => true;

    protected override string? CustomIconPath =>
        "res://TashkentSpire2/images/enchantment/ferment_enchantment.png";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromCard<Vodka>()
    ];
    
    public override bool CanEnchant(CardModel card)
    {
        if (card.Enchantment != null && (!IsStackable || card.Enchantment.GetType() != GetType()))
        {
            return false;
        }
        return true;
    }
    
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        if (side == this.Card.Owner.Creature.Side && combatState.RoundNumber <= 1)
        {
            await CardCmd.TransformTo<Vodka>(this.Card);
        }
    }
}