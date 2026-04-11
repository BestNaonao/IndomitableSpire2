using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndomitableSpire2.IndomitableSpire2Code.Powers;

public sealed class HypnotizedPower : IndomitablePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 用于精准区分“欲催眠状态”和“已催眠状态”
    public bool IsSleeping { get; private set; }
    
    // 【修改 2】动态文本切换：根据是否已入眠，返回不同的本地化键值
    protected override string SmartDescriptionLocKey => IsSleeping
        ? $"{Id.Entry}.smartDescriptionSleeping"
        : $"{Id.Entry}.smartDescriptionAwake";

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;

        if (!IsSleeping)
        {
            // Tm_1 结束：怪物进入催眠状态
            IsSleeping = true;
            await ApplySleepState();
        }
        else
        {
            // Tm_2 结束：怪物处于睡眠状态，回合结束时扣减层数
            await PowerCmd.Decrement(this);
            
            // 归零直接移除能力，触发 AfterRemoved 钩子进行清理；剩余层数大于 0，继续维持睡眠、重置格挡、清理多余眩晕特效
            if (Amount <= 0) 
                await PowerCmd.Remove(this);
            else 
                await ApplySleepState();
        }
    }

    // 提取出的核心逻辑复用方法
    private async Task ApplySleepState()
    {
        Flash();
        // 1. 核心修改：直接注入包含 SleepIntent 的 MoveState，完美替换原有的 Stun
        if (Owner.Monster is { MoveStateMachine: not null })
        {
            // 获取被打断前的状态 ID，以便苏醒后恢复原来的行动轨迹
            var nextMoveId = Owner.Monster.MoveStateMachine.StateLog.Last().Id;
            
            // 构建自定义的睡眠状态，传入官方原生的 SleepIntent
            var sleepState = new MoveState("HYPNOTIZED_SLEEP", SleepMove, new SleepIntent())
            {
                FollowUpStateId = nextMoveId,
                MustPerformOnceBeforeTransitioning = true
            };
            
            // 强制怪物立即执行该睡眠状态（这将自动把意图图标变更为 Zzz）
            Owner.Monster.SetMoveImmediate(sleepState);
        }
        
        await CreatureCmd.GainBlock(Owner, 50m, ValueProp.Move, null);
    }

    private async Task SleepMove(IReadOnlyList<Creature> targets) => await Task.CompletedTask;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.UnblockedDamage > 0 && IsSleeping)
        {
            await PowerCmd.Decrement(this);
            // 归零直接移除能力，依赖 AfterRemoved 钩子兜底
            if (Amount <= 0) await PowerCmd.Remove(this);
        }
    }

    // 利用原生生命周期钩子处理清理逻辑
    public override Task AfterRemoved(Creature oldOwner)
    {
        IsSleeping = false;
        return Task.CompletedTask;
    }
}