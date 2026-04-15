using TashkentSpire2.TashkentSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class EjectionStart : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/EjectionStart.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/EjectionStart.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/EjectionStart.png";
    
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Creature.Side)
        {
            Flash();
            await PowerCmd.Apply<DistancePower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
            await PowerCmd.Apply<BackAfterTurnPower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
        }
    }
}