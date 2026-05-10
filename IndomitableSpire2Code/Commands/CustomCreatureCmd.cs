using IndomitableSpire2.IndomitableSpire2Code.Powers;
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
    public static async Task<decimal> GainShield(Creature target, decimal amount, ValueProp props, CardPlay cardPlay)
    {
        // 1. 赋予底层受到增减益的真实的格挡值
        var blockAmount = await CreatureCmd.GainBlock(target, amount, props, cardPlay);
        
        // 2. 赋予同等层数的护盾能力（负责跨回合保留与追踪损耗）
        if (blockAmount > 0)
        {
            await PowerCmd.Apply<ShieldPower>(
                target: target, 
                amount: blockAmount, 
                applier: cardPlay.Card.Owner.Creature, 
                cardSource: cardPlay.Card
            );
        }
        
        return blockAmount;
    }
    
    public static async Task<decimal> GainShield(Creature target, BlockVar blockVar, CardPlay cardPlay)
    {
        return await GainShield(target, blockVar.BaseValue, blockVar.Props, cardPlay);
    }
}