using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class ArmouredCarrierPower : IndomitablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 每层提供的航空值
    private const int AviationPerStack = 10;
    // 每层提供的护盾值
    private const int ShieldPerStack = 3;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new ShieldVar(0M, ValueProp.Move), // 初始化为0，靠生命周期同步
        new CustomPowerVar<AviationPower>(0M) 
    ];
    
    // 同步变量值，用于更新卡牌/能力的文本描述
    private void SyncDynamicVars()
    {
        DynamicVars.Shield().BaseValue = Amount * ShieldPerStack;
        DynamicVars.Aviation().BaseValue = Amount * AviationPerStack;
    }
    
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncDynamicVars();
        return Task.CompletedTask;
    }
    
    public override Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
            SyncDynamicVars();
        return Task.CompletedTask;
    }
    
    // 核心回合开始逻辑
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0) return;
        
        Flash();
        
        // 1. 获得持续的护盾 (3 * Amount)
        await CustomCreatureCmd.GainShield(Owner, DynamicVars.Shield(), null, Owner);
        
        // 2. 查询上一回合是否受到了敌人的未格挡伤害
        // 条件：回合数 == 当前回合 - 1 且 受击者是自己 且 攻击者是敌人 且 总伤害 > 被格挡抵消的伤害
        var lastRound = CombatState.RoundNumber - 1;
        var tookUnblockedDamage = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Any(e => 
                e.RoundNumber ==  lastRound && 
                e.Receiver == Owner && 
                // e.Dealer is { IsEnemy: true } && 
                e.Result.UnblockedDamage > 0);
        
        // 如果没有破防，获得等同于层数(X)的航空
        if (!tookUnblockedDamage && lastRound > 0)
        {
            await PowerCmd.Apply<AviationPower>(
                target: Owner,
                amount: DynamicVars.Aviation().BaseValue,
                applier: Owner,
                cardSource: null
            );
        }
    }
}