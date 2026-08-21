using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace IndomitableSpire2.IndomitableSpire2Code.Relics;

public sealed class ShikikanNuigurumi : IndomitableRelic
{
    // 设为初始遗物
    public override RelicRarity Rarity => RelicRarity.Starter;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new HealVar(1M), 
        new MotivationGainVar(10M)
    ];
    
    // 核心接口实现：将其升级替换为“抱枕”
    public override RelicModel GetUpgradeReplacement() => ModelDb.Relic<ShikikanDakimakura>();
    
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side || Owner.Creature.IsDead)
            return;
        
        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        await PowerCmd.Apply<MotivationPower>(
            choiceContext, Owner.Creature, DynamicVars.MotivationGain().BaseValue, Owner.Creature, null);
    }
}