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

public sealed class HypnotizedPower : IndomitablePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override object InitInternalData() => new Data();
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new BlockVar(0M, ValueProp.Unpowered),
        new("DamageDecreasePercent", 0m)    // 用于动态显示减伤百分比
    ];
    
    // 可复用的计算逻辑：玩家固定 20，怪物根据玩家人数缩放
    private decimal CalculatedBlockAmount => 
        20m + 20m * (Owner.IsPlayer ? 0 : CombatState.PlayerCreatures.Count(c => c.IsAlive));
    
    // 怪物专用：区分“欲催眠状态”和“已催眠状态”
    private bool IsSleeping => !Owner.IsPlayer && (Owner.Monster?.IntendsToSleep() ?? false);
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsSleeping || Owner.IsPlayer 
        ? Array.Empty<IHoverTip>() : [CustomHoverTipFactory.FromIntent<SleepIntent>()];
    
    // 动态文本切换：根据身份和睡眠状态，返回不同的本地化键值
    protected override string SmartDescriptionLocKey => 
        Owner.IsPlayer ? $"{Id.Entry}.smartDescriptionPlayer" : $"{Id.Entry}.smartDescriptionAwake";
    
    // 动态减伤数值同步
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 核心修改：如果是玩家，且在回合内层数变动导致达到 5 层以上，立刻触发催眠！
        if (power == this)
        {
            DynamicVars["DamageDecreasePercent"].BaseValue = Amount * 5m;
            DynamicVars.Block.BaseValue = CalculatedBlockAmount;
            if (Owner.IsPlayer && !GetInternalData<Data>().PlayerIsSleeping) await CheckPlayerSleep();
        }
    }
    
    // ========== 核心机制 1：减伤 ==========
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, 
        ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        dealer == Owner && props.IsPoweredAttack() ? Math.Max(0m, 1m - Amount * 0.05m) : 1M;
    
    // ========== 核心机制 2：回合开始/内的玩家打断判定 ==========
    private async Task CheckPlayerSleep()
    {
        // 只有玩家、达到 5、且是自己的回合才会触发
        if (!Owner.IsPlayer || Amount < 5 || Owner.CombatState?.CurrentSide != Owner.Side) return;
        Flash();
        GetInternalData<Data>().PlayerIsSleeping = true;
        await CreatureCmd.GainBlock(Owner, CalculatedBlockAmount, ValueProp.Unpowered, null);
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -5, null, null);
        // 强行结束玩家回合
        if (Owner.Player != null) PlayerCmd.EndTurn(Owner.Player, false);
    }
    
    // 玩家回合开始时，检查是否带着 5 层以上
    public override async Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (Owner.IsPlayer && side == Owner.Side && participants.Contains(Owner)) await CheckPlayerSleep();
    }
    
    // ========== 核心机制 3：回合结束判定与自然扣减 ==========
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        var data = GetInternalData<Data>();
        if (Owner.IsPlayer)
        {
            // 玩家逻辑：在敌人回合结束时，像虚弱一样自然减少 1，如果应该忽略自然减少，则设置不应该忽略，否则自然减少
            if (side == CombatSide.Enemy)
            {
                if (data.PlayerIsSleeping) data.PlayerIsSleeping = false;
                else await PowerCmd.Decrement(this);
            }
        }
        else
        {
            // 怪物逻辑：在怪物回合结束时结算
            if (side != Owner.Side || !participants.Contains(Owner)) return;
            // 如果足够 5 层，强制睡眠
            if (Amount >= 5)
            {
                Flash();
                // 手动让怪物推进并生成下一个意图节点，防止苏醒后重复
                Owner.PrepareForNextTurn(CombatState.PlayerCreatures);
                Owner.SleepInternal(SleepMove, null);
                await CreatureCmd.GainBlock(Owner, CalculatedBlockAmount, ValueProp.Unpowered, null);
                await PowerCmd.ModifyAmount(choiceContext, this, -5, null, null);
            }
            else await PowerCmd.Decrement(this);
        }
    }
    
    private static async Task SleepMove(IReadOnlyList<Creature> targets) => await Task.CompletedTask;
    
    // ========== 核心机制 4：受伤破防扣层数 ==========
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 任何受到未被格挡的伤害，都会额外扣除 1
        if (target == Owner && result.UnblockedDamage > 0 && (Owner.IsPlayer || IsSleeping))
        {
            await PowerCmd.Decrement(this);
        }
    }
    
    // 用于记录玩家身上的催眠是否应该自然减少
    private class Data
    {
        public bool PlayerIsSleeping;
    }
}