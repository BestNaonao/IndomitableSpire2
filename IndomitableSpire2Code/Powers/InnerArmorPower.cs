using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

// 继承监听接口 IAfterShieldBrokenSubscriber
public sealed class InnerArmorPower : IndomitablePower, IAfterShieldBrokenSubscriber
{
    // 自身的标志位，用于记录是否因为护盾受击破碎，以及是否触发（防重入）
    private bool _shieldBrokenDuringDamage;
    private bool _triggered;
    
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner) _shieldBrokenDuringDamage = false;
        return Task.CompletedTask;
    }
    
    public Task AfterShieldBroken(Creature target)
    {
        if (target == Owner) _shieldBrokenDuringDamage = true;
        return Task.CompletedTask;
    }
    
    public override async Task AfterDamageReceivedLate(
        PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || (!result.WasBlockBroken && !_shieldBrokenDuringDamage) || _triggered) return;
        _triggered = true;
        _shieldBrokenDuringDamage = false;
        Flash();
        await CustomCreatureCmd.GainShield(choiceContext, Owner, Amount, ValueProp.Move, null, Owner);
        // 触发完毕后，功成身退，自我移除
        await PowerCmd.Remove(this);
    }
}