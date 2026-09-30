using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Animation;
using IndomitableSpire2.IndomitableSpire2Code.Cards.Abstracts;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using IndomitableSpire2.IndomitableSpire2Code.Registries;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.TestSupport;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class PlayerExtensions
{
    /// <summary>
    /// 获取玩家干劲的快捷方法
    /// </summary>
    public static int GetMotivationAmount(this Player player) => 
        player.Creature.GetPower<MotivationPower>()?.DisplayAmount ?? 0;
    
    /// <summary>
    /// 播放卡牌动画，并仅为不挠（含所有皮肤）播放台词和专属语音。
    /// 仅第 0 次结算播放台词和音效；后续重放只播放动画，且不查询或更新特殊台词记录。
    /// 特殊台词从卡牌属性读取，对话和专属音频共用每种卡牌每场战斗一次的记录。
    /// 只等待动画延迟，不等待台词或语音结束。
    /// </summary>
    /// <param name="player">打出卡牌的玩家。</param>
    /// <param name="cardPlay">单次打出卡牌的实例。</param>
    /// <param name="vfxColor">对话气泡颜色；null 时不显示对话。</param>
    /// <param name="volume">专属语音音量，默认 1；角色默认音效沿用原版音量。</param>
    /// <param name="animationTrigger">已注册的动画触发名（如 Cast、Attack），或 Spine 动作名（如 victory）；null 时不播放动画。</param>
    /// <param name="waitTime">动画结算前的等待秒数；null 时先按 Attack/Cast/PowerUp 触发名取角色延迟，其他动作按卡牌类型取攻击或施法延迟；没有卡牌类型时为 0。</param>
    /// <param name="duration">原版时长类型，默认按台词长度计算。</param>
    /// <param name="exactDurationSeconds">精确基础秒数；非 null 时优先于 duration，且不受快速模式缩短。</param>
    /// <param name="additionalDurationSeconds">基础时长之外追加的秒数。</param>
    /// <param name="isLooping">直接指定 Spine 动作名时是否循环；默认 false。已注册触发器保留其原有播放规则。</param>
    public static Task PlayIndomitableCardPresentation(
        this Player player,
        CardPlay cardPlay,
        VfxColor? vfxColor = VfxColor.Gold,
        float volume = 1f,
        string? animationTrigger = null,
        float? waitTime = null,
        bool isLooping = false,
        VfxDuration duration = VfxDuration.Custom,
        double? exactDurationSeconds = null,
        double additionalDurationSeconds = 0d)
    {
        if (player.Creature.IsDead) return Task.CompletedTask;
        var isFirstPlay = cardPlay.PlayIndex == 0;
        string? banterLocKey = null;
        string? specialAudioPath = null;
        
        // 重放与其他角色不读取特殊台词资源，也不消耗每场战斗的播放次数。
        if (isFirstPlay && player.Character is Indomitable && cardPlay.Card is IndomitableSpire2Card card)
        {
            if (IndomitableConfiguration.PlayDialogue && vfxColor != null)
                banterLocKey = card.SpecialLineBanterLocKey;
            if (IndomitableConfiguration.PlaySoundEffects)
                specialAudioPath = card.SpecialLineAudioPath;
            // 仅在实际有台词或音频可播放时登记；关闭仅一次开关时不访问注册表。
            if ((banterLocKey != null || specialAudioPath != null) &&
                IndomitableConfiguration.PlaySpecialLineOncePerCombat &&
                !SpecialLinesPlaybackRegistry.TryRecord(card.Id.Entry))
            {
                banterLocKey = null;
                specialAudioPath = null;
            }
        }
        // 播放动画
        var animationTask = Task.CompletedTask;
        if (animationTrigger != null)
        {
            // 第 0 次未播放特殊音频时回退默认音效；后续重放始终使用无音效的动画入口。传入结算时的卡牌类型，让自定义动作也能使用对应的角色延迟。
            animationTask = player.TriggerCardAnimation(animationTrigger, waitTime, 
                useDefaultSfx: isFirstPlay && specialAudioPath == null, isLooping: isLooping, cardType: cardPlay.Card.Type);
        }
        // 播放对话框和音频音效
        if (banterLocKey != null && vfxColor is { } color)
        {
            var talk = CustomTalkCmd.Play(new LocString("cards", banterLocKey), player.Creature, color);
            if (exactDurationSeconds is { } seconds)
                talk.WithExactDuration(seconds);
            else
                talk.WithDuration(duration);
            talk.WithAdditionalDuration(additionalDurationSeconds).Execute();
        }
        if (specialAudioPath != null) SfxCmd.Play(specialAudioPath, volume);
        return animationTask;
    }
    
    /// <summary>
    /// 播放已注册的动画触发器或现有 Spine 动作，并等待指定的结算延迟。
    /// 也可用于战后等非卡牌表现：此底层入口不检查卡牌特殊动画开关，不播放台词、不登记特殊台词次数。
    /// </summary>
    /// <param name="player">播放动作的玩家；死亡或缺少可视化节点时直接结束。</param>
    /// <param name="triggerName">区分大小写的已注册触发名（Cast、Attack 等），或原始 Spine 动作名（victory 等）。</param>
    /// <param name="waitTime">非负且有限的普通模式等待秒数；null 时依次按原版触发名、cardType 推断，二者均未提供有效依据时为 0。显式 0 不会被自动延迟覆盖。</param>
    /// <param name="useDefaultSfx">是否使用原版动画音效入口。只有原版认识的触发名有默认音效；自定义动作不借用 Cast 音效。</param>
    /// <param name="isLooping">只作用于直接指定的 Spine 动作：false 播放一次后回 Idle；true 循环到其他触发器打断。</param>
    /// <param name="cardType">卡牌类型，未命中 Attack/Cast/PowerUp 且未指定 waitTime 时决定使用何种等待时间。</param>
    /// <returns>仅代表结算等待的 Task，不代表动画或音效播放完毕。waitTime=0 仍会触发动画。</returns>
    /// <remarks>
    /// 快速模式等待 min(waitTime * 0.5, 0.25) 秒；即时模式沿用原版跳过等待。
    /// 改变 waitTime 不会截断动作，也不会锁定动画，Hit、Dead 等原版触发器仍可接管。
    /// 已注册触发器的循环/后继状态由原状态机决定，isLooping 不改写它们。
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">显式 waitTime 为负数、NaN 或无穷大。</exception>
    public static Task TriggerCardAnimation(this Player player, string triggerName, 
        float? waitTime = null, bool useDefaultSfx = true, bool isLooping = false, CardType? cardType = null)
    {
        if (waitTime is { } seconds && (!float.IsFinite(seconds) || seconds < 0f))
            throw new ArgumentOutOfRangeException(nameof(waitTime), "Animation wait time must be finite and non-negative.");
        var creature = player.Creature;
        if (creature.IsDead || string.IsNullOrWhiteSpace(triggerName)) return Task.CompletedTask;
        var creatureNode = creature.GetCreatureNode();
        if (creatureNode == null)
        {
            if (!TestMode.IsOn && CombatManager.Instance.IsInProgress)
                Log.Error($"Attempted to play animation on creature {creature} but its creature node doesn't exist!");
            return Task.CompletedTask;
        }
        if (!TryResolveCardAnimationTrigger(creatureNode, triggerName, out var resolvedTrigger, isLooping))
            return Task.CompletedTask;
        // 解析成功后计算动画延迟时间，未指定 waitTime 时默认使用原版延迟或者默认值 0
        var delay = waitTime ?? triggerName switch
        {
            CreatureAnimator.attackTrigger => player.Character.AttackAnimDelay,
            CreatureAnimator.castTrigger or CreatureAnimator.powerUpTrigger => player.Character.CastAnimDelay,
            _ => cardType switch
            {
                CardType.Attack => player.Character.AttackAnimDelay,
                null or CardType.None => 0f,
                _ => player.Character.CastAnimDelay
            }
        };
        if (useDefaultSfx) return CreatureCmd.TriggerAnim(creature, resolvedTrigger, delay);
        // BaseLib 和 RitsuLib 都在 SetAnimationTrigger 上分发自定义动画，保留此入口及原版的快速模式等待。
        creatureNode.SetAnimationTrigger(resolvedTrigger);
        return Cmd.CustomScaledWait(Mathf.Min(delay * 0.5f, 0.25f), delay);
    }
    
    /// <summary>
    /// 解析卡牌动画名称，但不播放、不等待、不处理音效。
    /// 已有全局/当前状态触发器优先；真实 Spine 动作交给共用适配层注册；其余名称原样转交外部动画分发。
    /// </summary>
    /// <param name="node">玩家的生物节点；非 Spine 模型保留原名称，交给 BaseLib 等动画分发器。</param>
    /// <param name="name">区分大小写的逻辑触发名或 Spine 动作名；空白名称无法解析。</param>
    /// <param name="trigger">成功时可传给 SetAnimationTrigger 的名称；失败时为空。</param>
    /// <param name="isLooping">原始 Spine 动作是否循环；不改变已有触发器的行为，也不影响其他库的动画。</param>
    /// <returns>可以向动画入口分发时为 true。外部分发名称返回 true 不保证该外部动画实际存在。</returns>
    /// <remarks>共用适配层按动作名和循环方式复用状态，避免同一动作的单次与循环版本相互覆盖。</remarks>
    public static bool TryResolveCardAnimationTrigger(NCreature node, string name, out string trigger, bool isLooping = false)
    {
        trigger = string.Empty;
        if (string.IsNullOrWhiteSpace(name)) return false;
        trigger = name;
        // 非 Spine 角色继续交由其他库的动画分发处理。
        if (!node.HasSpineAnimation) return true;
        if (CreatureAnimation.GetAnimator(node) is not {} animator)
        {
            trigger = string.Empty;
            return false;
        }
        if (CreatureAnimation.HasTrigger(animator, name)) return true;
        // 未匹配 Spine 动作时保留原触发名，允许其他库继续处理。
        return node.Visuals.SpineBody?.HasAnimation(name) != true || 
               CreatureAnimation.TryResolveSpineAnimation(node, name, out trigger, isLooping);
    }
}