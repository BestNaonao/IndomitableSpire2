using MegaCrit.Sts2.Core.Combat;
using TashkentSpire2.TashkentSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

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
    
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player == base.Owner && combatState.CurrentSide == base.Owner.Creature.Side)
        {
            Flash();
            if (combatState.RoundNumber <= 1)
            {
                await PowerCmd.Apply<DistancePower>(base.Owner.Creature, 13m, base.Owner.Creature, null);
            }
            else
            {
                await PowerCmd.Apply<DistancePower>(base.Owner.Creature, 3m, base.Owner.Creature, null);
            }
            await PowerCmd.Apply<BackAfterTurnPower>(base.Owner.Creature, 3m, base.Owner.Creature, null);
        }
    }
}