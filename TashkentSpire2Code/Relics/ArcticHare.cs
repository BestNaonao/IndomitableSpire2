using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class ArcticHare : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/ArcticHare.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/ArcticHare.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/ArcticHare.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VigorPower>(9M)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VigorPower>()
    ];
    
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(base.Owner.Creature))
        {
            Flash();
            await PowerCmd.Apply<VigorPower>(new ThrowingPlayerChoiceContext(), base.Owner.Creature, base.DynamicVars["VigorPower"].IntValue, base.Owner.Creature, null);
        }
    }
}