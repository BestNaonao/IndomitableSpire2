using TashkentSpire2.TashkentSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rooms;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class EngineBoost : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/EngineBoost.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/EngineBoost.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/EngineBoost.png";
    
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
        {
            Flash();
            await PowerCmd.Apply<DistancePower>(base.Owner.Creature, 10m, base.Owner.Creature, null);
        }
    }
    
    public override async Task AfterEnergyReset(Player player)
    {
        if (player == base.Owner)
        {
            Flash();
            await PowerCmd.Apply<DistancePower>(base.Owner.Creature, 3m, base.Owner.Creature, null);
            await PowerCmd.Apply<BackAfterTurnPower>(base.Owner.Creature, 3m, base.Owner.Creature, null);
        }
    }
}