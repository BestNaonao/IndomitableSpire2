using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class CreatureExtension
{
    /// <summary>
    /// 参考官方代码，实现生物睡眠的内部逻辑，仅限敌人
    /// </summary>
    public static void SleepInternal(this Creature creature, Func<IReadOnlyList<Creature>, Task> sleepMove,
        string? nextMoveId)
    {
        if (creature.Monster is not { } monster)
            throw new InvalidOperationException("Can't send a player to sleep.");
        if (creature.CombatState == null || creature.IsDead)
            return;
        if (string.IsNullOrEmpty(nextMoveId) && monster.MoveStateMachine is { } machine)
            nextMoveId = machine.StateLog.Last().Id;
        monster.SetMoveImmediate(new MoveState("SLEEP", sleepMove, new SleepIntent())
        {
            FollowUpStateId = nextMoveId,   // 被打断前的状态 ID，苏醒后恢复原来的行动轨迹
            MustPerformOnceBeforeTransitioning = true
        });
    }
}