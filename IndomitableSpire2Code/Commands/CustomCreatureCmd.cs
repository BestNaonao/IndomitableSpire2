using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Commands;

public static class CustomCreatureCmd
{
    /// <summary>
    /// 将目标自身的生命一比一转为格挡，向下取整且至少保留 1 点生命。
    /// 这不是伤害或普通格挡获取：不经过伤害、生命损失、格挡数值修正，但保留生命变化、格挡获取通知和战斗历史。返回实际转化量。
    /// </summary>
    public static async Task<int> ConvertHpToBlock(Creature target, decimal amount)
    {
        var combatState = target.CombatState;
        if (combatState == null || !combatState.IsLiveCombat() || CombatManager.Instance.IsOverOrEnding || 
            target.IsDead || amount < 1m)
            return 0;
        // 与 Creature.GainBlockInternal 的上限一致，避免扣除无法转成格挡的生命。
        const int maxBlock = 999999999;
        var converted = TransferableAmount();
        if (converted <= 0) return 0;
        // 前置钩子可能改变目标的生命、格挡或战斗状态，因此需要再次计算和校验。
        await Hook.BeforeBlockGained(combatState, target, converted, ValueProp.Unpowered, null);
        if (target.CombatState != combatState || target.IsDead || CombatManager.Instance.IsOverOrEnding)
            return 0;
        converted = TransferableAmount();
        if (converted <= 0) return 0;
        // 在任何异步后置钩子之前完成两项修改，避免将转化误记为额外攻击。
        target.SetCurrentHpInternal(target.CurrentHp - converted);
        target.GainBlockInternal(converted);
        CombatManager.Instance.History.BlockGained(combatState, target, converted, ValueProp.Unpowered, null);
        SfxCmd.Play("event:/sfx/block_gain");
        VfxCmd.PlayOnCreatureCenter(target, "vfx/vfx_block");
        await Hook.AfterCurrentHpChanged(combatState.RunState, combatState, target, -converted);
        await Hook.AfterBlockGained(combatState, target, converted, ValueProp.Unpowered, null);
        return converted;
        
        // 方法内部定义计算转化量的方法，取输入 amount、可消耗血量和可获得格挡之间的最小值。
        int TransferableAmount() => (int)Math.Min(decimal.Floor(amount),
            Math.Min(Math.Max(0, target.CurrentHp - 1), maxBlock - target.Block));
    }
    
    /// <summary>
    /// 赋予目标护盾（附带同等数值的真实格挡与跨回合保留能力）
    /// </summary>
    public static async Task<decimal> GainShield(
        PlayerChoiceContext choiceContext, 
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
                choiceContext: choiceContext, 
                target: target, 
                amount: blockAmount, 
                applier: applier ?? cardPlay?.Card.Owner.Creature, 
                cardSource: cardPlay?.Card
            );
        }
        
        return blockAmount;
    }
    
    public static async Task<decimal> GainShield(
        PlayerChoiceContext choiceContext, 
        Creature target, 
        BlockVar blockVar, 
        CardPlay? cardPlay,
        Creature? applier = null)
    {
        return await GainShield(choiceContext, target, blockVar.BaseValue, blockVar.Props, cardPlay, applier);
    }
    
    /// <summary>
    /// 判断目标生物上一回合是否符合“优雅”的条件（即：在上一回合存在，且没有受到任何未被格挡的伤害）
    /// </summary>
    public static bool MeetsElegance(this Creature creature)
    {
        var combatState = creature.CombatState;
        if (combatState == null || creature.Player == null) return false;
        
        // 计算回合数，第一回合没有上一回合，按规则无法触发优雅
        if (creature.Player.PlayerCombatState?.TurnNumber <= 1) return false;
        
        var tookUnblockedDamage = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Any(e => 
                e.Receiver == creature && 
                e.Result.UnblockedDamage > 0 && 
                e.HappenedLastPlayerTurn(creature.Player));
        return !tookUnblockedDamage;
    }
}
