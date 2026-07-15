using IndomitableSpire2.IndomitableSpire2Code.Enums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class AirRaidGuidancePower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override object InitInternalData() => new Data();
    
    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker != Owner || !command.DamageProps.IsPoweredAttack())
            return Task.CompletedTask;
        
        var internalData = GetInternalData<Data>();
        
        // 核心限制：只加成“舰载机牌”发起的攻击
        if (internalData.CommandToModify != null || command.ModelSource is not CardModel card || !card.Tags.Contains(IndomitableTags.CarrierAircraft))
            return Task.CompletedTask;
        
        internalData.CommandToModify = command;
        internalData.AmountWhenAttackStarted = Amount;
        return Task.CompletedTask;
    }
    
    // 【核心修复】：重构加成逻辑，支持无命令的预览状态
    public override decimal ModifyDamageAdditive(
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // 1. 基础校验
        // 2. 预览与实战的双重校验：无论是否有攻击命令，加成的前提必须是“舰载机牌”
        if (Owner != dealer || !props.IsPoweredAttack() ||
            cardSource == null || !cardSource.Tags.Contains(IndomitableTags.CarrierAircraft)) return 0M;
        
        var internalData = GetInternalData<Data>();
        // 3. 拦截器：如果当前有记录在案的真实攻击动作，且发起者不是当前这张牌，则不生效
        return internalData.CommandToModify == null || cardSource == internalData.CommandToModify.ModelSource ?
            Amount: 0M;
    }
    
    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        var internalData = GetInternalData<Data>();
        if (command != internalData.CommandToModify) return;
        await PowerCmd.ModifyAmount(choiceContext, this, -internalData.AmountWhenAttackStarted, null, null);
    }
    
    private class Data
    {
        public AttackCommand? CommandToModify;
        public int AmountWhenAttackStarted;
    }
}