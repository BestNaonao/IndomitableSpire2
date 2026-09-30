using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace IndomitableSpire2.IndomitableSpire2Code.Animation;

public static class HypnotizedAnimation
{
    private const string AnimationGroup = "Hypnotized";
    private const string DefaultAnimationName = "sleep";
    
    /// <summary>在玩家催眠爆发时尝试启动循环动作；不等待，也不改变催眠的玩法状态。</summary>
    /// <param name="creature">触发催眠爆发的玩家生物。</param>
    /// <returns>开关开启、玩家存活且拥有指定动作或 sleep，并成功发送动画触发器时为 true。</returns>
    public static bool TryPlay(Creature creature)
    {
        if (!IndomitableConfiguration.PlayHypnotizedAnimation) return false;
        if (!creature.IsPlayer || !creature.IsAlive || creature.GetCreatureNode() is not { } node) return false;
        var animationName = (creature.Player?.Character as IHypnotizedAnimationProvider)?.HypnotizedAnimationName;
        if (!CreatureAnimation.TryResolveSpineAnimation(node, animationName, out var trigger,
                isLooping: true, fallbackAnimationName: DefaultAnimationName, animationGroup: AnimationGroup)) return false;
        node.SetAnimationTrigger(trigger);
        return true;
    }
    
    /// <summary>在该玩家回合开始时恢复尚未被其他动作打断的催眠动画，与能力是否仍存在无关。</summary>
    /// <param name="creature">本次开始行动的生物；非玩家或已死亡时跳过。</param>
    internal static void WakeAtTurnStart(Creature creature)
    {
        // 不受播放开关影响，确保关闭设置前已经开始的催眠动画仍能正常结束。
        if (!creature.IsPlayer || !creature.IsAlive || creature.GetCreatureNode() is not { } node) return;
        CreatureAnimation.RestoreIdleIfPlaying(node, AnimationGroup);
    }
}