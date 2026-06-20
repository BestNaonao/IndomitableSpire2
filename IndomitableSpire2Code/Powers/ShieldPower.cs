using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class ShieldPower : IndomitablePower, IBlockRetentionProvider
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 监听受伤：当本体受到伤害且该伤害被格挡吸收时，扣除等量的护盾层数
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var damageAbsorbed = result.BlockedDamage;
        
        if (target != Owner || damageAbsorbed <= 0 || !props.IsPoweredAttack()) return;
        
        // 扣除护盾层数，最多扣到 0
        var absorb = Math.Min(Amount, damageAbsorbed);
        await PowerCmd.ModifyAmount(choiceContext, this, -absorb, dealer, cardSource);
    }
    
    public override bool ShouldClearBlock(Creature creature) => Owner != creature;
    
    // IBlockRetentionProvider 接口：提供跨回合保留格挡的功能
    public bool ShouldAggregate => true;
    
    public int CalculateRetainedBlock(AbstractModel sourceModel, Creature creature) => Amount;
    
    public void OnRetentionTriggered(AbstractModel sourceModel, Creature creature) => Flash();
}