using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace IndomitableSpire2.IndomitableSpire2Code.Animation;

/// <summary>
/// 共用的 Spine 动画适配层：集中访问原版状态机、查找动作、注册状态及识别播放来源。
/// 不处理配置、音效或结算等待；实际播放仍通过 NCreature.SetAnimationTrigger 分发。
/// </summary>
public static class CreatureAnimation
{
    private const string TriggerPrefix = "IndomitableSpire2.Animation";
    private const string DefaultAnimationGroup = "Custom";
    
    // 将对原版私有字段的依赖集中在一处，游戏更新时只需检查这里。
    private static readonly AccessTools.FieldRef<NCreature, CreatureAnimator?> SpineAnimator =
        AccessTools.FieldRefAccess<NCreature, CreatureAnimator?>("_spineAnimator");
    private static readonly AccessTools.FieldRef<CreatureAnimator, AnimState> AnyState =
        AccessTools.FieldRefAccess<CreatureAnimator, AnimState>("_anyState");
    private static readonly AccessTools.FieldRef<CreatureAnimator, AnimState> CurrentState =
        AccessTools.FieldRefAccess<CreatureAnimator, AnimState>("_currentState");
    
    /// <summary>取得已初始化的 Spine 状态机；非 Spine 或无状态机时返回 null。</summary>
    internal static CreatureAnimator? GetAnimator(NCreature node) => node.HasSpineAnimation ? SpineAnimator(node) : null;
    
    /// <summary>同时检查全局触发器和当前状态的局部分支，避免将已有触发器误当作原始动作名。</summary>
    internal static bool HasTrigger(CreatureAnimator animator, string trigger) =>
        animator.HasTrigger(trigger) || CurrentState(animator).HasTrigger(trigger);
    
    /// <summary>
    /// 将真实 Spine 动作解析为可播放的触发器，必要时注册一次状态；此方法本身不播放动画。
    /// 非循环动作自动排队返回角色原有的 Idle 状态；循环动作不设置 NextState，等待外部触发器打断。
    /// </summary>
    /// <param name="node">拥有可视化模型的生物节点。</param>
    /// <param name="animationName">Spine 文件中的动作名，区分大小写；不是 Cast 等逻辑触发名。</param>
    /// <param name="trigger">成功时返回可传给 SetAnimationTrigger 的名称；失败时为空。</param>
    /// <param name="isLooping">是否循环播放。默认 false：播放一次后回到 Idle。</param>
    /// <param name="fallbackAnimationName">首选动作为空或不存在时尝试的备用动作；null 表示不回退。</param>
    /// <param name="animationGroup">状态来源标记，默认 Custom。催眠使用独立标记，以便只唤醒催眠状态。</param>
    /// <returns>找到动作且能够准备对应状态时为 true；无 Spine、无动作或单次播放缺少 Idle 状态时为 false。</returns>
    /// <remarks>
    /// 触发器按来源、循环方式和实际动作名区分。同一个 sleep 可同时用于普通表现和催眠，互不混淆。
    /// 重复调用复用已有状态；改变备用动作或循环方式会选择另一个状态，不修改正在使用的状态。
    /// </remarks>
    public static bool TryResolveSpineAnimation(
        NCreature node, string? animationName, out string trigger, bool isLooping = false,
        string? fallbackAnimationName = null, string animationGroup = DefaultAnimationGroup)
    {
        trigger = string.Empty;
        if (GetAnimator(node) is not {} animator || node.Visuals.SpineBody is not {} controller) return false;
        // animationName 为空字符串或者不存在该名称的动作时，回退到 fallbackAnimationName 动作
        if (string.IsNullOrWhiteSpace(animationName) || !controller.HasAnimation(animationName))
        {
            animationName = fallbackAnimationName;
            if (string.IsNullOrWhiteSpace(animationName) || !controller.HasAnimation(animationName)) return false;
        }
        // 组装成为自定义的拥有独立命名空间的解析动作名称
        var resolvedTrigger = $"{TriggerPrefix}.{animationGroup}.{(isLooping ? "Loop" : "Once")}.{animationName}";
        if (!animator.HasTrigger(resolvedTrigger))
        {
            // 复用原版的 Idle 状态，而不是假定所有角色都使用 normal 或 idle_loop。
            var idle = isLooping ? null : AnyState(animator).CallTrigger(CreatureAnimator.idleTrigger);
            if (!isLooping && idle == null) return false;
            animator.AddAnyState(resolvedTrigger, new ManagedAnimState(animationName, isLooping, animationGroup)
            {
                NextState = idle
            });
        }
        trigger = resolvedTrigger;
        return true;
    }
    
    /// <summary>仅在当前仍播放指定来源的托管状态时恢复 Idle，不打断已经接管的受击、死亡等状态。</summary>
    /// <param name="node">待恢复的生物节点。</param>
    /// <param name="animationGroup">注册状态时使用的来源标记。</param>
    /// <returns>确实发送了 Idle 触发器时为 true。</returns>
    public static bool RestoreIdleIfPlaying(NCreature node, string animationGroup)
    {
        if (GetAnimator(node) is not {} animator || CurrentState(animator) is not ManagedAnimState state ||
            state.AnimationGroup != animationGroup || !HasTrigger(animator, CreatureAnimator.idleTrigger)) return false;
        node.SetAnimationTrigger(CreatureAnimator.idleTrigger);
        return true;
    }
    
    private sealed class ManagedAnimState(string name, bool isLooping, string animationGroup) : AnimState(name, isLooping)
    {
        public string AnimationGroup { get; } = animationGroup;
    }
}