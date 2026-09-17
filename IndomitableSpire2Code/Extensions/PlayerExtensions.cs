using Godot;
using IndomitableSpire2.IndomitableSpire2Code.Character;
using IndomitableSpire2.IndomitableSpire2Code.Commands;
using IndomitableSpire2.IndomitableSpire2Code.Configuration;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
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
    /// 重放时仍播放动画，但不重复台词和专属语音；只等待动画延迟，不等待台词或语音结束。
    /// </summary>
    /// <param name="player">打出卡牌的玩家。</param>
    /// <param name="cardPlay">单次打出卡牌的实例。</param>
    /// <param name="cardEntry">卡牌 Id.Entry，读取 cards 表的 {cardEntry}.banter；null 时不显示对话。</param>
    /// <param name="vfxColor">对话气泡颜色；null 时不显示对话。</param>
    /// <param name="sfxPath">专属语音路径；有动画时替换默认音效。null 时动画使用角色默认音效，无动画时不播放音效。</param>
    /// <param name="volume">专属语音音量，默认 1；角色默认音效沿用原版音量。</param>
    /// <param name="animationTrigger">动画触发名，如 Cast、Attack、PowerUp；null 时不播放动画。</param>
    /// <param name="animationDelay">动画结算前的等待秒数；null 时 Attack 使用角色 AttackAnimDelay，其他动画使用 CastAnimDelay。</param>
    /// <param name="duration">原版时长类型，默认按台词长度计算。</param>
    /// <param name="exactDurationSeconds">精确基础秒数；非 null 时优先于 duration，且不受快速模式缩短。</param>
    /// <param name="additionalDurationSeconds">基础时长之外追加的秒数。</param>
    public static Task PlayIndomitableCardPresentation(
        this Player player,
        CardPlay cardPlay,
        string? cardEntry = null,
        VfxColor? vfxColor = VfxColor.Gold,
        string? sfxPath = null,
        float volume = 1f,
        string? animationTrigger = null,
        float? animationDelay = null,
        VfxDuration duration = VfxDuration.Custom,
        double? exactDurationSeconds = null,
        double additionalDurationSeconds = 0d)
    {
        if (player.Creature.IsDead) return Task.CompletedTask;
        // 处理动画
        var isIndomitable = player.Character is Indomitable;
        var animationTask = Task.CompletedTask;
        if (animationTrigger != null)
        {
            var waitTime = animationDelay ?? (animationTrigger == "Attack"
                ? player.Character.AttackAnimDelay
                : player.Character.CastAnimDelay);
            // 音效开关关闭或玩家为其他角色仍播放默认音效；不挠指定专属语音后则不播放默认音效，重放时也不回退。
            animationTask = player.TriggerCardAnimation(animationTrigger, waitTime, 
                useDefaultSfx: !isIndomitable || !IndomitableConfiguration.PlaySoundEffects || sfxPath == null);
        }
        // 台词和语音为不挠专属，卡牌重放时也不播放。
        if (!isIndomitable || cardPlay.PlayIndex != 0) return animationTask;
        // 参数中条目和颜色不为空时播放台词
        if (IndomitableConfiguration.PlayDialogue && cardEntry != null && vfxColor is { } color)
        {
            var talk = CustomTalkCmd.Play(new LocString("cards", $"{cardEntry}.banter"), player.Creature, color);
            if (exactDurationSeconds is { } seconds)
                talk.WithExactDuration(seconds);
            else
                talk.WithDuration(duration);
            talk.WithAdditionalDuration(additionalDurationSeconds).Execute();
        }
        // 参数中音频路径不为空时播放语音
        if (IndomitableConfiguration.PlaySoundEffects && sfxPath != null)
            SfxCmd.Play(sfxPath, volume);
        return animationTask;
    }
    
    private static Task TriggerCardAnimation(this Player player, string triggerName, float waitTime, bool useDefaultSfx)
    {
        if (useDefaultSfx) return CreatureCmd.TriggerAnim(player.Creature, triggerName, waitTime);
        var creature = player.Creature;
        var creatureNode = creature.GetCreatureNode();
        if (creatureNode == null)
        {
            if (!TestMode.IsOn && CombatManager.Instance.IsInProgress)
                Log.Error($"Attempted to play animation on creature {creature} but its creature node doesn't exist!");
            return Task.CompletedTask;
        }
        // BaseLib 和 RitsuLib 都在 SetAnimationTrigger 上分发自定义动画，保留此入口及原版的快速模式等待。
        creatureNode.SetAnimationTrigger(triggerName);
        return Cmd.CustomScaledWait(Mathf.Min(waitTime * 0.5f, 0.25f), waitTime);
    }
}