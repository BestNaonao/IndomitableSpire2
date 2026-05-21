using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class CustomCreatureCmd
{
    /// <summary>
    /// 赋予目标护盾（附带同等数值的真实格挡与跨回合保留能力）
    /// </summary>
    public static async Task<decimal> GainShield(
        Creature target, 
        decimal amount, 
        ValueProp props, 
        CardPlay? cardPlay,
        Creature? applier = null)
    {
        // 1. 赋予底层受到增减益的真实的格挡值
        var blockAmount = await CreatureCmd.GainBlock(target, amount, props, cardPlay);
        
        // 2. 赋予同等层数的护盾能力（负责跨回合保留与追踪损耗）
        if (blockAmount > 0)
        {
            await PowerCmd.Apply<ShieldPower>(
                target: target, 
                amount: blockAmount, 
                applier: applier ?? cardPlay?.Card.Owner.Creature, 
                cardSource: cardPlay?.Card
            );
        }
        
        return blockAmount;
    }
    
    public static async Task<decimal> GainShield(
        Creature target, 
        BlockVar blockVar, 
        CardPlay? cardPlay,
        Creature? applier = null)
    {
        return await GainShield(target, blockVar.BaseValue, blockVar.Props, cardPlay, applier);
    }
    
    /// <summary>
    /// 判断目标生物上一回合是否符合“优雅”的条件（即：在上一回合存在，且没有受到任何未被格挡的伤害）
    /// </summary>
    public static bool MeetsElegance(this Creature creature)
    {
        var combatState = creature.CombatState;
        if (combatState == null) return false;
        
        // 计算回合数，第一回合没有上一回合，按规则无法触发优雅
        var lastRound = combatState.RoundNumber - 1;
        if (lastRound <= 0) return false;
        
        var tookUnblockedDamage = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Any(e => 
                e.RoundNumber == lastRound && 
                e.Receiver == creature && 
                e.Result.UnblockedDamage > 0);
        return !tookUnblockedDamage;
    }
}