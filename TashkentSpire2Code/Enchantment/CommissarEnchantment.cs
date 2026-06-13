using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Enchantment;

public sealed class CommissarEnchantment : CustomEnchantmentModel
{
    public override bool ShowAmount => false;

    public override bool HasExtraCardText => true;

    protected override string? CustomIconPath =>
        "res://TashkentSpire2/images/enchantment/commissar_enchantment.png";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Innate),
        HoverTipFactory.FromKeyword(CardKeyword.Retain)
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1)
    ];
    
    protected override void OnEnchant()
    {
        Card.AddKeyword(CardKeyword.Innate);
        Card.AddKeyword(CardKeyword.Retain);
    }
    
    public override bool CanEnchant(CardModel card)
    {
        if (card.Enchantment != null && (!IsStackable || card.Enchantment.GetType() != GetType()))
        {
            return false;
        }
        return true;
    }
    
    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == base.Card.Owner.Creature.Side)
        {
            CardPile handPile = PileType.Hand.GetPile(base.Card.Owner);
            if (handPile.Cards != null && handPile.Cards.Contains(base.Card))
            {
                await Cmd.Wait(0.25f);
                await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, base.Card.Owner.Creature, base.DynamicVars.Energy.BaseValue, base.Card.Owner.Creature, base.Card);
            }
        }
    }
}