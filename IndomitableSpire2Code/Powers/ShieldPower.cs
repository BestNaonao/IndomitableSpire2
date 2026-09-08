using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Enums;
using IndomitableSpire2.IndomitableSpire2Code.Hooks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class ShieldPower : IndomitablePower, IBlockRetentionProvider, IAfterBlockLostSubscriber
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 监听受伤：当本体受到伤害且该伤害被格挡吸收时，扣除等量的护盾层数
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, 
        Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        var damageAbsorbed = result.BlockedDamage;
        if (target != Owner || damageAbsorbed <= 0) return;
        // 扣除护盾层数，最多扣到 0
        var absorb = Math.Min(Amount, damageAbsorbed);
        if (absorb > 0) await PowerCmd.ModifyAmount(choiceContext, this, -absorb, dealer, cardSource);
    }
    
    // 监听非伤害 LoseBlock：以指令实际移除的格挡值同步扣除护盾。
    public async Task AfterBlockLost(
        PlayerChoiceContext choiceContext, Creature target, int amount, Creature? remover, BlockLossReason reason)
    {
        if (reason != BlockLossReason.Normal || target != Owner || amount <= 0) return;
        var lost = Math.Min(Amount, amount);
        if (lost > 0) await PowerCmd.ModifyAmount(choiceContext, this, -lost, remover, null);
    }
    
    public override bool ShouldClearBlock(Creature creature) => Owner != creature;
    
    // IBlockRetentionProvider 接口：提供跨回合保留格挡的功能
    public bool ShouldAggregate => true;
    
    public int CalculateRetainedBlock(AbstractModel sourceModel, Creature creature) => Amount;
    
    public Task OnRetentionTriggered(AbstractModel sourceModel, Creature creature)
    {
        Flash();
        return Task.CompletedTask;
    }
    
    // 当护盾层数跌至 0，被引擎彻底移除时触发
    public override async Task AfterRemoved(Creature owner)
    {
        // 只有层数耗尽才是“破碎”；驱散、死亡等仍有正层数的移除不广播。如果使用卡牌移除，请务必使用 PowerCmd.ModifyAmount。
        if (Amount <= 0) await CustomHook.AfterShieldBroken(owner);
        await base.AfterRemoved(owner); // 调用基类方法保证底层逻辑完整
    }
}