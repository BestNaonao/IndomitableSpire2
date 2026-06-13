using MegaCrit.Sts2.Core.Combat;
using TashkentSpire2.TashkentSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class EngineBoost : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/EngineBoost.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/EngineBoost.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/EngineBoost.png";
    
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player == base.Owner && combatState.CurrentSide == base.Owner.Creature.Side)
        {
            Flash();
            await PowerCmd.Apply<DistancePower>(choiceContext, base.Owner.Creature, 3m, base.Owner.Creature, null);
            await PowerCmd.Apply<BackAfterTurnPower>(choiceContext, base.Owner.Creature, 3m, base.Owner.Creature, null);
        }
    }
}