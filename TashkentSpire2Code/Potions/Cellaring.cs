using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Cards.Token;

namespace TashkentSpire2.TashkentSpire2Code.Potions;

public sealed class Cellaring : TashkentPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;
     
    public override TargetType TargetType => TargetType.Self;
     
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new CardsVar(3)
    ];
     
    public override string CustomPackedImagePath => "res://TashkentSpire2/images/potions/mark_potion.png";
    public override string CustomPackedOutlinePath => "res://TashkentSpire2/images/potions/mark_potion_outline.png";
     
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        ArgumentNullException.ThrowIfNull(base.Owner.Creature.CombatState);
        await Vodka.CreateInHand(base.Owner, base.DynamicVars.Cards.IntValue, base.Owner.Creature.CombatState);
    }
}