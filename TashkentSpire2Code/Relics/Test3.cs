using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public sealed class Test3 : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/Thruster.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/Thruster.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/Thruster.png";
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new MaxHpVar(4m)
    ];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.ForEnergy(this)
    ];
    
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Creature.Side && combatState.RoundNumber <= 1)
        {
            await CreatureCmd.GainMaxHp(base.Owner.Creature, base.DynamicVars.MaxHp.BaseValue);
        }
    }
    
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == base.Owner.Creature.Side)
        {
            Flash();
            var ctx = new HookPlayerChoiceContext(this, LocalContext.NetId!.Value, combatState, GameActionType.Combat);
            await PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, base.Owner);
            await CreatureCmd.LoseMaxHp(ctx, base.Owner.Creature, 1m, isFromCard: false);
        }
    }
}