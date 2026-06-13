using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class Fuel : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/Fuel.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/Fuel.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/Fuel.png";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VigorPower>(2M)];

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner == base.Owner)
        {
            Flash();
            await PowerCmd.Apply<VigorPower>(choiceContext, base.Owner.Creature, base.DynamicVars["VigorPower"].IntValue, base.Owner.Creature, null);
        }
    }
}