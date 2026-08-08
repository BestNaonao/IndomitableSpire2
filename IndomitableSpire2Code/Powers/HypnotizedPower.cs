using IndomitableSpire2.IndomitableSpire2Code.Extensions;
using IndomitableSpire2.IndomitableSpire2Code.Localization.HoverTips;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
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
        20m + 20m * (Owner.IsPlayer || Owner.IsPet ? 0 : CombatState.PlayerCreatures.Count(c => c.IsAlive));
    
    // 仅限敌人使用的睡眠判定
    private bool IsEnemySleeping => Owner.IsEnemy && (Owner.Monster?.IntendsToSleep() ?? false);
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => IsEnemySleeping || Owner.IsPlayer || Owner.IsPet 
        ? Array.Empty<IHoverTip>() : [CustomHoverTipFactory.FromIntent<SleepIntent>()];
    
    // 动态文本切换：根据身份和睡眠状态，返回不同的本地化键值
    protected override string SmartDescriptionLocKey => Owner.IsPlayer ? $"{Id.Entry}.smartDescriptionPlayer" :
        Owner.IsPet ? $"{Id.Entry}.smartDescriptionPet" : $"{Id.Entry}.smartDescriptionEnemy";
    
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
    
    // ========== 核心机制 3：奥斯提专项控制 ==========
    // 拦截卡牌打出：达标时禁止主人打出奥斯提攻击牌及“牺牲”
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) => 
        !Owner.IsPet || Amount < 5 || card.Owner != Owner.PetOwner || 
        (!card.Tags.Contains(CardTag.OstyAttack) && card is not Sacrifice);
    
    // 拦截召唤指令：达标时禁止主人召唤奥斯提（或增加生命上限）
    public override decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source) => 
        !Owner.IsPet || Amount < 5 || summoner != Owner.PetOwner ? amount : 0m;
    
    // ========== 核心机制 4：多端回合结束判定与自然扣减 ==========
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // 结算逻辑：严格判定在敌方的回合结束时结算
        if (side != CombatSide.Enemy) return;
        if (Owner.IsPlayer)
        {
            var data = GetInternalData<Data>();
            // 玩家逻辑：如果应该忽略自然减少，则设置不应该忽略，否则像虚弱一样自然减少 1
            if (data.PlayerIsSleeping) data.PlayerIsSleeping = false;
            else await PowerCmd.Decrement(this);
        }
        else if (Owner.IsPet)
        {
            if (Amount < 5) await PowerCmd.Decrement(this);
            else await PowerCmd.ModifyAmount(choiceContext, this, -5, null, null);
        }
        else if (Owner.IsEnemy)
        {
            if (!participants.Contains(Owner)) return;
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
    
    // ========== 核心机制 5：受伤破防扣减 ==========
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 除了清醒的敌人，任何受到未被格挡的伤害，都会额外扣除 1
        if (target == Owner && result.UnblockedDamage > 0 && (IsEnemySleeping || Owner.IsPlayer || Owner.IsPet))
            await PowerCmd.Decrement(this);
    }
    
    // 用于记录玩家身上的催眠是否应该自然减少
    private class Data
    {
        public bool PlayerIsSleeping;
    }
}