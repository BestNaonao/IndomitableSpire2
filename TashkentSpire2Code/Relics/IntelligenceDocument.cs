using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using TashkentSpire2.TashkentSpire2Code.Powers;

namespace TashkentSpire2.TashkentSpire2Code.Relics;

public class IntelligenceDocument : TashkentRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    
    protected override string BigIconPath => 
        "res://TashkentSpire2/images/relics/big/EjectionStart.png";
    public override string PackedIconPath => 
        "res://TashkentSpire2/images/relics/packed/EjectionStart.png";
    protected override string PackedIconOutlinePath => 
        "res://TashkentSpire2/images/relics/outline/EjectionStart.png";
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MarkPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<MarkPower>(4m)
    ];

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)
    {
        if (side != base.Owner.Creature.Side || combatState.RoundNumber > 1)
        {
            return;
        }
        Flash();
        foreach (Creature hittableEnemy2 in base.Owner.Creature.CombatState!.HittableEnemies)
        {
            await PowerCmd.Apply<MarkPower>(hittableEnemy2, base.DynamicVars["TashkentSpire2-Mark"].BaseValue, base.Owner.Creature, null);
        }
    }
}