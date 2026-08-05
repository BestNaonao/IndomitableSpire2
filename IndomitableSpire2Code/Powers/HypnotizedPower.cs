using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.HoverTips;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class HypnotizedPower : DynamicVarSyncPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 自定义的变量名称前缀
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new BlockVar(0M, ValueProp.Unpowered),
        new("DamageDecreasePercent", 0m)    // 用于动态显示减伤百分比
    ];
    
    // 可复用的静态计算逻辑
    private static decimal CalculatedBlockAmount(PowerModel power, Creature? creature) =>
        20m + 20m * power.CombatState.PlayerCreatures.Count(c => c.IsAlive);
    
    // 用于精准区分“欲催眠状态”和“已催眠状态”
    private bool IsSleeping => Owner.Monster?.IntendsToSleep() ?? false;
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsSleeping 
        ? Array.Empty<IHoverTip>() : [CustomHoverTipFactory.FromIntent<SleepIntent>()];
    
    // 动态文本切换：根据是否已入眠，返回不同的本地化键值
    protected override string SmartDescriptionLocKey => IsSleeping
        ? $"{Id.Entry}.smartDescriptionSleeping"
        : $"{Id.Entry}.smartDescriptionAwake";
    
    // 动态减伤数值同步
    protected override void SyncDynamicVars()
    {
        DynamicVars["DamageDecreasePercent"].BaseValue = Amount * 5m;
        DynamicVars.Block.BaseValue = CalculatedBlockAmount(this, null);
    }
    
    // ========== 核心机制 1：减伤 ==========
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, 
        ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        dealer == Owner && props.IsPoweredAttack() ? Math.Max(0m, 1m - Amount * 0.05m) : 1M;
    
    // ========== 核心机制 2：回合结束判定与层数扣减 ==========
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !participants.Contains(Owner)) return;
        // 如果足够 5 层，强制睡眠
        if (Amount >= 5)
        {
            Flash();
            // 【核心修复】：在强行塞入 Sleep 之前，手动让怪物推进并生成下一个意图节点！
            Owner.PrepareForNextTurn(CombatState.PlayerCreatures);
            // 核心修改：调用自定义的生物睡眠扩展
            Owner.SleepInternal(SleepMove, null);
            // 获得格挡
            await CreatureCmd.GainBlock(Owner, CalculatedBlockAmount(this, null), ValueProp.Unpowered, null);
            await PowerCmd.ModifyAmount(choiceContext, this, -5, null, null);
        }
        // 否则自然减少 1 层
        else await PowerCmd.Decrement(this);
    }
    
    private static async Task SleepMove(IReadOnlyList<Creature> targets) => await Task.CompletedTask;
    
    // ========== 核心机制 3：受伤破防扣层数 ==========
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0 && IsSleeping)
        {
            await PowerCmd.Decrement(this);
        }
    }
}