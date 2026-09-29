using HarmonyLib;
using IndomitableSpire2.IndomitableSpire2Code.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace IndomitableSpire2.IndomitableSpire2Code.Animation;

public static class HypnotizedAnimation
{
    private const string Trigger = "IndomitableSpire2.Hypnotized";
    private const string DefaultAnimationName = "sleep";
    
    private static readonly AccessTools.FieldRef<NCreature, CreatureAnimator?> SpineAnimator =
        AccessTools.FieldRefAccess<NCreature, CreatureAnimator?>("_spineAnimator");
    private static readonly AccessTools.FieldRef<CreatureAnimator, AnimState> CurrentState =
        AccessTools.FieldRefAccess<CreatureAnimator, AnimState>("_currentState");
    
    public static bool TryPlay(Creature creature)
    {
        if (!IndomitableConfiguration.PlayHypnotizedAnimation) return false;
        if (!creature.IsPlayer || !creature.IsAlive || 
            creature.GetCreatureNode() is not { HasSpineAnimation: true } node) return false;
        var controller = node.Visuals.SpineBody;
        var animator = SpineAnimator(node);
        if (controller == null || animator == null) return false;
        
        var animationName = (creature.Player?.Character as IHypnotizedAnimationProvider)?.HypnotizedAnimationName;
        if (string.IsNullOrWhiteSpace(animationName) || !controller.HasAnimation(animationName))
        {
            animationName = DefaultAnimationName;
            if (!controller.HasAnimation(animationName)) return false;
        }
        
        // 非重复地添加状态。然后交给状态机播放，不排队返回待机；原版 Hit / Idle / Dead 等触发器仍可正常切换状态。
        if (!animator.HasTrigger(Trigger))
            animator.AddAnyState(Trigger, new HypnotizedAnimState(animationName));
        node.SetAnimationTrigger(Trigger);
        return true;
    }
    
    internal static void WakeAtTurnStart(Creature creature)
    {
        // 不受播放开关影响，确保关闭设置前已经开始的催眠动画仍能正常结束。
        if (!creature.IsPlayer || !creature.IsAlive || creature.GetCreatureNode() is not { } node) return;
        var animator = SpineAnimator(node);
        // 只恢复仍处于催眠动画的玩家，避免打断已经开始的受击、死亡或其他动画。
        if (animator != null && CurrentState(animator) is HypnotizedAnimState)
            node.SetAnimationTrigger(CreatureAnimator.idleTrigger);
    }
    
    // 用独立状态标识催眠，避免将同名的营地/战后休息动画误认成催眠。
    private sealed class HypnotizedAnimState(string animationName) : AnimState(animationName, isLooping: true);
}